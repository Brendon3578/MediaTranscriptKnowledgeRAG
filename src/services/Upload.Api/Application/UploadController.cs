using Microsoft.AspNetCore.Mvc;
using Upload.Api.Application.Interfaces;
using Upload.Api.Domain.DTOs;
using Upload.Api.Domain.Enum;
using Shared.Exceptions;

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
            if (file == null || file.Length == 0)
                throw new ValidationException("Arquivo não enviado");

            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new ValidationException($"Tipo de arquivo não suportado: {file.ContentType}");

            var isEnumParsed = Enum.TryParse<WhisperModelTypesEnum.GgmlType>(model, true, out var parsedModel);

            if (isEnumParsed == false && string.IsNullOrEmpty(model))
            {
                parsedModel = WhisperModelTypesEnum.GgmlType.Medium;
            }
            else if (isEnumParsed == false)
            {
                throw new NotFoundException("Modelo do whisper não encontrado");
            }

            var uploadMediaDto = await _uploadService.UploadFileAsync(file, parsedModel.ToString(), ct);

            return AcceptedAtAction(
                nameof(GetStatus),
                new { id = uploadMediaDto.MediaId },
                uploadMediaDto
            );
        }

        [HttpGet("{id}/status")]
        public async Task<ActionResult<MediaUploadDto>> GetStatus(Guid id, CancellationToken ct)
        {
            var uploadMediaDto = await _uploadService.GetStatus(id, ct);

            if (uploadMediaDto == null)
                throw new NotFoundException("Media não encontrada");

            return Ok(uploadMediaDto);
        }

        [HttpGet("GetAllMedia")]
        public async Task<ActionResult<IReadOnlyList<MediaListItemDto>>> GetAllMedia(CancellationToken ct)
        {
            var list = await _uploadService.GetAllMediaAsync(ct);
            return Ok(list);
        }

        [HttpGet("transcribed")]
        public async Task<ActionResult<PagedResponseDto<TranscribedMediaDto>>> GetTranscribedMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _uploadService.GetTranscribedMediaAsync(page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("transcribed/{id}")]
        public async Task<ActionResult<TranscribedMediaDto>> GetTranscribedMediaById(Guid id, CancellationToken ct)
        {
            var result = await _uploadService.GetTranscribedMediaByIdAsync(id, ct);
            if (result == null)
                throw new NotFoundException("Media não encontrada ou sem transcrição");

            return Ok(result);
        }
    }
}
