using System.Threading;
using System.Threading.Tasks;

namespace MediaTranscription.Worker.Infrastructure.Interfaces
{
    public record MediaMetadata(double DurationSeconds, string? AudioCodec, int? SampleRate);

    public interface IMediaMetadataExtractor
    {
        Task<MediaMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken);
    }
}
