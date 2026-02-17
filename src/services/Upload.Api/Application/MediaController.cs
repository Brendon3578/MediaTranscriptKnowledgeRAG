using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
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
        private readonly GetTranscriptionSegmentsUseCase _getTranscriptionSegmentsUseCase;
        private readonly ILogger<MediaController> _logger;

        public MediaController(
            IUploadService uploadService,
            GetMediaStatusUseCase getMediaStatusUseCase,
            GetTranscriptionSegmentsUseCase getTranscriptionSegmentsUseCase,
            ILogger<MediaController> logger)
        {
            _uploadService = uploadService;
            _getMediaStatusUseCase = getMediaStatusUseCase;
            _getTranscriptionSegmentsUseCase = getTranscriptionSegmentsUseCase;
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

        [HttpGet("{id}/stream")]
        public async Task Stream(Guid id, CancellationToken ct)
        {
            try
            {
                var mediaStatus = await _getMediaStatusUseCase.ExecuteAsync(id, ct);
                if (mediaStatus == null)
                {
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    Response.ContentType = "application/json";

                    var errorJson = JsonSerializer.Serialize(new ErrorResponseRequest("Media not found"));
                    await Response.WriteAsync(errorJson, ct);
                    await Response.Body.FlushAsync(ct);
                    return;
                }

                Response.Headers["Content-Type"] = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Connection"] = "keep-alive";

                Response.StatusCode = StatusCodes.Status200OK;

                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var lastIndex = 0;

                while (!ct.IsCancellationRequested)
                {
                    var segments = await _getTranscriptionSegmentsUseCase
                        .GetSegmentsAfterIndex(id, lastIndex, ct);

                    if (segments.Count > 0)
                    {
                        foreach (var segment in segments)
                        {
                            var dto = new TranscriptionSegmentStreamDto
                            {
                                MediaId = segment.MediaId,
                                Index = segment.SegmentIndex,
                                StartSeconds = segment.StartSeconds,
                                EndSeconds = segment.EndSeconds,
                                Text = segment.Text
                            };

                            var json = JsonSerializer.Serialize(dto, jsonOptions);

                            await Response.WriteAsync("event: segment\n", ct);
                            await Response.WriteAsync($"data: {json}\n\n", ct);
                            await Response.Body.FlushAsync(ct);

                            if (segment.SegmentIndex > lastIndex)
                            {
                                lastIndex = segment.SegmentIndex;
                            }
                        }
                    }

                    var currentStatus = await _getMediaStatusUseCase.ExecuteAsync(id, ct);
                    if (currentStatus == null ||
                        string.Equals(currentStatus.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        await Response.WriteAsync("event: completed\n", ct);
                        await Response.WriteAsync("data: {}\n\n", ct);
                        await Response.Body.FlushAsync(ct);
                        break;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao transmitir segmentos de transcrição para mídia {MediaId}", id);
            }
        }
    }
}
