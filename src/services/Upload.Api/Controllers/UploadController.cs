using Microsoft.AspNetCore.Mvc;
using Upload.Api.Infrastructure;
using Upload.Api.Infrastructure.DTOs;
using Upload.Api.Infrastructure.Entities;
using Upload.Api.Infrastructure.Enum;
using Upload.Api.Infrastructure.FileStorage;
using Upload.Api.Messaging;

namespace Upload.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        private readonly UploadDbContext _context;
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<UploadController> _logger;

        private const int MaxRequestSizeLimitInBytes = 500_000_000; // 500 MB

        private static readonly string[] AllowedContentTypes =
        {
            "audio/mpeg", "audio/wav", "audio/mp4", "audio/ogg",
            "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
            "video/x-matroska"
        };

        public UploadController(
            IFileStorage fileStorage,
            UploadDbContext context,
            IMessagePublisher publisher,
            ILogger<UploadController> logger
        )
        {
            _fileStorage = fileStorage;
            _context = context;
            _publisher = publisher;
            _logger = logger;
        }

        [HttpPost]
        [RequestSizeLimit(MaxRequestSizeLimitInBytes)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new ErrorResponseRequest("Arquivo não enviado"));

                if (!AllowedContentTypes.Contains(file.ContentType))
                    return BadRequest(new ErrorResponseRequest($"Tipo de arquivo não suportado: {file.ContentType}"));

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


                await _publisher.PublishAsync(uploadedEvent, ct);

                // Retorna 202 Accepted
                return AcceptedAtAction(
                    nameof(GetStatus),
                    new { id = media.Id },
                    new
                    {
                        mediaId = media.Id,
                        fileName = media.FileName,
                        status = media.Status.ToString(),
                        uploadedAt = media.CreatedAt
                    }
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar upload");
                return StatusCode(500, new { error = "Erro ao processar upload" });
            }
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
        {
            var media = await _context.Media.FindAsync(new object[] { id }, ct);

            if (media == null)
                return NotFound(new { error = "Media não encontrada" });

            return Ok(new
            {
                mediaId = media.Id,
                fileName = media.FileName,
                status = media.Status.ToString(),
                uploadedAt = media.CreatedAt,
                updatedAt = media.UpdatedAt
            });
        }
    }

}