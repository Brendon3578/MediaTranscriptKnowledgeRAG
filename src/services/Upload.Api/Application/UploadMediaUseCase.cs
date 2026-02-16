using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using Upload.Api.Application.Interfaces;
using Upload.Api.Domain;
using Upload.Api.Domain.DTOs;
using Upload.Api.Infrastructure.Persistence;

namespace Upload.Api.Application
{
    public class UploadMediaUseCase : IUploadService
    {
        private readonly IFileStorageFacade _fileStorage;
        private readonly UploadDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<UploadMediaUseCase> _logger;
        private readonly IConfiguration _configuration;

        public UploadMediaUseCase(
            IFileStorageFacade fileStorage,
            UploadDbContext context,
            IEventPublisher eventPublisher,
            ILogger<UploadMediaUseCase> logger,
            IConfiguration configuration
        )
        {
            _fileStorage = fileStorage;
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<MediaUploadDto> UploadFileAsync(IFormFile file, string? model, CancellationToken ct)
        {
            _logger.LogInformation(
                   "Iniciando upload de {FileName} ({Size} bytes)",
                   file.FileName,
                   file.Length
               );

            // Salva o arquivo localmente / em bucket s3
            string filePath;

            using (var stream = file.OpenReadStream())
            {
                filePath = await _fileStorage.SaveFileAsync(stream, file.FileName,
                    file.ContentType, ct);
            }

            // Entidade no banco
            var media = new MediaEntity
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                Status = MediaStatus.Uploaded,
                CreatedAt = DateTime.UtcNow
            };

            _context.Media.Add(media);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Media {MediaId} salva no banco", media.Id);

            // Determina modelo Whisper (default Medium)
            var whisperModel = string.IsNullOrWhiteSpace(model) ? "Medium" : model;

            // Publica evento
            var uploadedEvent = new MediaUploadedEvent
            {
                MediaId = media.Id,
                FilePath = media.FilePath,
                ContentType = media.ContentType,
                FileName = media.FileName,
                FileSizeBytes = media.FileSizeBytes,
                UploadedAt = media.CreatedAt,
                TranscriptionModel = whisperModel // Novo campo
            };

            var routingKey = _configuration["RabbitMq:RoutingKey"] ?? "media.uploaded";

            await _eventPublisher.PublishAsync(uploadedEvent,
                routingKey: routingKey,
                ct
            );

            var dto = new MediaUploadDto
            {
                MediaId = media.Id,
                FileName = media.FileName,
                Status = media.Status.ToString(),
                UploadedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt,
                Model = whisperModel
            };

            return dto;
        }

        public async Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct)
        {
             var media = await _context.Media.FindAsync(new object[] { id }, ct);
             if (media == null) return null;

             return new MediaUploadDto
             {
                 MediaId = media.Id,
                 FileName = media.FileName,
                 Status = media.Status.ToString(),
                 UploadedAt = media.CreatedAt,
                 UpdatedAt = media.UpdatedAt,
                 Model = media.Transcription?.ModelName,
             };
        }

        public async Task<IReadOnlyList<MediaListItemDto>> GetAllMediaAsync(CancellationToken ct)
        {
            var list = await _context.Media
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MediaListItemDto
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    ContentType = m.ContentType,
                    FileSizeBytes = m.FileSizeBytes,
                    Status = m.Status.ToString(),
                    DurationSeconds = m.DurationSeconds,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return list;
        }

        public async Task<PagedResponseDto<TranscribedMediaDto>> GetTranscribedMediaAsync(int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Media
                .AsNoTracking()
                .Where(m => m.Transcription != null);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new TranscribedMediaDto
                {
                    MediaId = m.Id,
                    FileName = m.FileName,
                    MediaType = m.ContentType,
                    Duration = m.DurationSeconds != null ? (int) m.DurationSeconds : 0,
                    Status = m.Status.ToString(),
                    TranscriptionText = m.Transcription!.Text,
                    Model = m.Transcription!.ModelName ?? "",
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return new PagedResponseDto<TranscribedMediaDto>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
        }

        public async Task<TranscribedMediaDto?> GetTranscribedMediaByIdAsync(Guid id, CancellationToken ct)
        {
            var media = await _context.Media
                .AsNoTracking()
                .Where(m => m.Id == id && m.Transcription != null)
                .Select(m => new TranscribedMediaDto
                {
                    MediaId = m.Id,
                    FileName = m.FileName,
                    MediaType = m.ContentType,
                    Duration = m.DurationSeconds != null ? (int)m.DurationSeconds : 0,
                    Status = m.Status.ToString(),
                    TranscriptionText = m.Transcription!.Text,
                    Model = m.Transcription!.ModelName ?? "",
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            return media;
        }

        public async Task<MediaDeletionSummaryDto?> DeleteMediaAsync(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("Iniciando exclusão de mídia {MediaId}", id);

            var media = await _context.Media
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (media == null)
            {
                _logger.LogWarning("Mídia não encontrada para exclusão {MediaId}", id);
                return null;
            }

            var filePath = media.FilePath;

            int embeddingsDeleted = 0;
            int segmentsDeleted = 0;
            int transcriptionsDeleted = 0;
            int mediaDeleted = 0;


            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                embeddingsDeleted = await _context.Embeddings
                    .Where(e => e.MediaId == id)
                    .ExecuteDeleteAsync(ct);

                segmentsDeleted = await _context.TranscriptionSegments
                    .Where(s => s.MediaId == id)
                    .ExecuteDeleteAsync(ct);

                transcriptionsDeleted = await _context.Transcriptions
                    .Where(t => t.MediaId == id)
                    .ExecuteDeleteAsync(ct);

                mediaDeleted = await _context.Media
                    .Where(m => m.Id == id)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation(
                    "Exclusão em cascata concluída para mídia {MediaId}: {EmbeddingsDeleted} embeddings, {SegmentsDeleted} segmentos, {TranscriptionsDeleted} transcrições, {MediaDeleted} registros de mídia",
                    id,
                    embeddingsDeleted,
                    segmentsDeleted,
                    transcriptionsDeleted,
                    mediaDeleted);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Transação de exclusão confirmada para mídia {MediaId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar transação de exclusão para mídia {MediaId}", id);
                await transaction.RollbackAsync(ct);
                throw;
            }

            var fileDeleted = false;

            try
            {
                fileDeleted = await _fileStorage.DeleteFileAsync(filePath, ct);
                _logger.LogInformation(
                    "Resultado da exclusão do arquivo físico para mídia {MediaId}: FileDeleted={FileDeleted}",
                    id,
                    fileDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir arquivo físico para mídia {MediaId}", id);
            }

            return new MediaDeletionSummaryDto
            {
                MediaId = id,
                EmbeddingsDeleted = embeddingsDeleted,
                SegmentsDeleted = segmentsDeleted,
                TranscriptionsDeleted = transcriptionsDeleted,
                FileDeleted = fileDeleted
            };
        }
    }
}
