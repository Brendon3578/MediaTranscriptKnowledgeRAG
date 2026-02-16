using Microsoft.AspNetCore.Mvc;
using Upload.Api.Application.Interfaces;
using Upload.Api.Domain.DTOs;

namespace Upload.Api.Application
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IUploadService _uploadService;
        private readonly GetMediaStatusUseCase _getMediaStatusUseCase;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            IUploadService uploadService,
            GetMediaStatusUseCase getMediaStatusUseCase,
            ILogger<MediaController> logger)
        {
            _uploadService = uploadService;
            _getMediaStatusUseCase = getMediaStatusUseCase;
            _logger = logger;
        }

        [HttpGet("{id}/transcription-status")]
        public async Task<ActionResult<MediaStatusDto>> GetTranscriptionStatus(Guid id, CancellationToken ct)
        {
            var status = await _getMediaStatusUseCase.ExecuteAsync(id, ct);

            if (status == null)
            {
                return NotFound(new ErrorResponseRequest("Media não encontrada"));
            }

            return Ok(status);
        }

        [HttpDelete("{mediaId}")]
        public async Task<IActionResult> Delete(Guid mediaId, CancellationToken ct)
        {
            try
            {
                var result = await _uploadService.DeleteMediaAsync(mediaId, ct);

                if (result == null)
                    return NotFound(new ErrorResponseRequest("Media não encontrada"));

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir mídia {MediaId}", mediaId);
                return StatusCode(500, new ErrorResponseRequest("Erro ao excluir mídia"));
            }
        }
    }
}
