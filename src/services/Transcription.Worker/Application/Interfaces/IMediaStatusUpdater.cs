using MediaTranscription.Worker.Domain;

namespace MediaTranscription.Worker.Application.Interfaces
{
    public interface IMediaStatusUpdater
    {
        Task<bool> UpdateStatusAsync(Guid mediaId, MediaStatus newStatus, MediaStatus expectedCurrentStatus, CancellationToken cancellationToken = default);
    }
}
