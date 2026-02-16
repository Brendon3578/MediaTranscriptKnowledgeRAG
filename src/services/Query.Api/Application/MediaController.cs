using Microsoft.AspNetCore.Mvc;

namespace Query.Api.Application
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly GetMediaStatusUseCase _getMediaStatusUseCase;

        public MediaController(GetMediaStatusUseCase getMediaStatusUseCase)
        {
            _getMediaStatusUseCase = getMediaStatusUseCase;
        }

        [HttpGet("{id}/status")]
        public async Task<ActionResult<MediaStatusResponse>> GetStatus(Guid id)
        {
            var status = await _getMediaStatusUseCase.ExecuteAsync(id);

            if (status == null)
            {
                return NotFound(new { error = $"Media with ID {id} not found." });
            }

            return Ok(status);
        }
    }
}
