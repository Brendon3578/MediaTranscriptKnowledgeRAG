using Microsoft.Extensions.Options;
using Shared.Contracts.Events;
using Upload.Api.Configuration;
using Upload.Api.Data;
using Upload.Api.Interfaces;
using Upload.Api.Models.DTOs;
using Upload.Api.Models.Entities;

namespace Upload.Api.Services
{
    public class LocalUploadService : IUploadService
    {
        private readonly IFileStorageFacade _fileStorage;
        private readonly UploadDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LocalUploadService> _logger;
        private readonly RabbitMqOptions _rabbitMqOptions;

        public LocalUploadService(
            IFileStorageFacade fileStorage,
            UploadDbContext context,
            IEventPublisher eventPublisher,
            ILogger<LocalUploadService> logger,
            IOptions<RabbitMqOptions> rabbitMqOptions
        )
        {
            _fileStorage = fileStorage;
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _rabbitMqOptions = rabbitMqOptions.Value;
        }



        public async Task<MediaUploadDto> UploadFileAsync(IFormFile file, CancellationToken ct)
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

            var media = new Media
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

            // Publica evento
            var uploadedEvent = new MediaUploadedEvent
            {
                MediaId = media.Id,
                FilePath = media.FilePath,
                ContentType = media.ContentType,
                FileName = media.FileName,
                FileSizeBytes = media.FileSizeBytes,
                UploadedAt = media.CreatedAt
            };


            await _eventPublisher.PublishAsync(uploadedEvent,
                routingKey: _rabbitMqOptions.RoutingKey,
                ct
            );


            var dto = new MediaUploadDto
            {
                MediaId = media.Id,
                FileName = media.FileName,
                Status = media.Status.ToString(),
                UploadedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt,
            };

            return dto;
        }

        public async Task<MediaUploadDto?> GetStatus(Guid id, CancellationToken ct)
        {
            var media = await _context.Media.FindAsync(new object[] { id }, ct);

            if (media == null)
                return null;

            var dto = new MediaUploadDto
            {
                MediaId = media.Id,
                FileName = media.FileName,
                Status = media.Status.ToString(),
                UploadedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt,
            };
            return dto;
        }
    }
}
