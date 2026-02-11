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
    }
}
