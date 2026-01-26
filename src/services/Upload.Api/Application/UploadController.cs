using Microsoft.AspNetCore.Mvc;
using Upload.Api.Application.Interfaces;
using Upload.Api.Domain.DTOs;

namespace Upload.Api.Application
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IUploadService _uploadService;

        private const int MaxRequestSizeLimitInBytes = 500_000_000; // 500 MB

        private static readonly string[] AllowedContentTypes =
        {
            "audio/mpeg", "audio/wav", "audio/mp4", "audio/ogg",
            "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
            "video/x-matroska"
        };

        public UploadController(
            IUploadService uploadService,
            ILogger<UploadController> logger
        )
        {
            _uploadService = uploadService;
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

                var uploadMediaDto = await _uploadService.UploadFileAsync(file, ct);

                // Retorna 202 Accepted
                return AcceptedAtAction(
                    nameof(GetStatus),
                    new { id = uploadMediaDto.MediaId },
                    uploadMediaDto
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
            var uploadMediaDto = await _uploadService.GetStatus(id, ct);

            if (uploadMediaDto == null)
                return NotFound(new { error = "Media não encontrada" });

            return Ok(uploadMediaDto);
        }
    }

}