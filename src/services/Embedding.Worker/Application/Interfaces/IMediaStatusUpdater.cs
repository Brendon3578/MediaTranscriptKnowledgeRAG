using MediaEmbedding.Worker.Domain.Models;

namespace MediaEmbedding.Worker.Application.Interfaces
{
    public interface IMediaStatusUpdater
    {
        Task<bool> UpdateStatusAsync(Guid mediaId, MediaStatus newStatus, MediaStatus expectedCurrentStatus, CancellationToken cancellationToken = default);
        Task UpdateProgressAsync(Guid mediaId, float progress, CancellationToken cancellationToken = default);
        Task UpdateFinalStateAsync(Guid mediaId, MediaStatus finalStatus, CancellationToken cancellationToken = default);
    }
}
