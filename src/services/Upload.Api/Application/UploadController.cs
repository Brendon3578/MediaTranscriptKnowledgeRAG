using Microsoft.AspNetCore.Mvc;
using Upload.Api.Application.Interfaces;
using Upload.Api.Domain.DTOs;
using Upload.Api.Domain.Enum;

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
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? model, CancellationToken ct)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new ErrorResponseRequest("Arquivo não enviado"));

                if (!AllowedContentTypes.Contains(file.ContentType))
                    return BadRequest(new ErrorResponseRequest($"Tipo de arquivo não suportado: {file.ContentType}"));

                var isEnumParsed = Enum.TryParse<WhisperModelTypesEnum.GgmlType>(model, true, out var parsedModel);

                if (isEnumParsed == false && string.IsNullOrEmpty(model))
                {
                    parsedModel = WhisperModelTypesEnum.GgmlType.Medium; // Valor padrão
                }
                else if (isEnumParsed == false)
                {
                    return BadRequest("Modelo do whisper não encontrado");
                }


                var uploadMediaDto = await _uploadService.UploadFileAsync(file, parsedModel.ToString(), ct);

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

        [HttpGet("GetAllMedia")]
        public async Task<IActionResult> GetAllMedia(CancellationToken ct)
        {
            try
            {
                var list = await _uploadService.GetAllMediaAsync(ct);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar mídias");
                return StatusCode(500, new { error = "Erro ao listar mídias" });
            }
        }

        [HttpGet("transcribed")]
        public async Task<ActionResult<PagedResponseDto<TranscribedMediaDto>>> GetTranscribedMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            try
            {
                var result = await _uploadService.GetTranscribedMediaAsync(page, pageSize, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar mídias transcritas");
                return StatusCode(500, new { error = "Erro ao listar mídias transcritas" });
            }
        }
    }
}
