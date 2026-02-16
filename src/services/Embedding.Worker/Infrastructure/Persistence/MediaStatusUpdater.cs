using MediaEmbedding.Worker.Application.Interfaces;
using MediaEmbedding.Worker.Domain.Models;
using MediaEmbedding.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaEmbedding.Worker.Infrastructure.Persistence
{
    public class MediaStatusUpdater : IMediaStatusUpdater
    {
        private readonly EmbeddingDbContext _context;
        private readonly ILogger<MediaStatusUpdater> _logger;

        public MediaStatusUpdater(EmbeddingDbContext context, ILogger<MediaStatusUpdater> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> UpdateStatusAsync(Guid mediaId, MediaStatus newStatus, MediaStatus expectedCurrentStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var rowsAffected = await _context.Media
                    .Where(m => m.Id == mediaId && m.Status == expectedCurrentStatus)
                    .ExecuteUpdateAsync(calls => calls
                        .SetProperty(m => m.Status, newStatus)
                        .SetProperty(m => m.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                if (rowsAffected == 0)
                {
                    // Check idempotency
                    var isAlreadyUpdated = await _context.Media
                        .AnyAsync(m => m.Id == mediaId && m.Status == newStatus, cancellationToken);

                    if (isAlreadyUpdated)
                    {
                        _logger.LogInformation("Media {MediaId} is already in status {Status}. Skipping update.", mediaId, newStatus);
                        return true;
                    }

                    _logger.LogWarning("Failed to update Media {MediaId} to status {NewStatus}. Current status is not {ExpectedStatus} or media not found.", mediaId, newStatus, expectedCurrentStatus);
                    return false;
                }

                _logger.LogInformation("Media {MediaId} status updated to {Status}.", mediaId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for Media {MediaId}", mediaId);
                throw;
            }
        }

        public async Task UpdateProgressAsync(Guid mediaId, float progress, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Media
                    .Where(m => m.Id == mediaId)
                    .ExecuteUpdateAsync(calls => calls
                        .SetProperty(m => m.EmbeddingProgressPercent, progress)
                        .SetProperty(m => m.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for Media {MediaId}", mediaId);
            }
        }

        public async Task UpdateFinalStateAsync(Guid mediaId, MediaStatus finalStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Media
                    .Where(m => m.Id == mediaId)
                    .ExecuteUpdateAsync(calls => calls
                        .SetProperty(m => m.Status, finalStatus)
                        .SetProperty(m => m.EmbeddingProgressPercent, finalStatus == MediaStatus.Completed ? 100f : null)
                        .SetProperty(m => m.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                _logger.LogInformation("Media {MediaId} finalized with status {Status}.", mediaId, finalStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing state for Media {MediaId}", mediaId);
            }
        }
    }
}
