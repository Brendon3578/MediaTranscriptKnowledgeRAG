using FFMpegCore;
using MediaTranscription.Worker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaTranscription.Worker.Infrastructure
{
    public class MediaMetadataExtractor : IMediaMetadataExtractor
    {
        private readonly ILogger<MediaMetadataExtractor> _logger;

        public MediaMetadataExtractor(ILogger<MediaMetadataExtractor> logger)
        {
            _logger = logger;
        }

        public async Task<MediaMetadata?> ExtractMetadataAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Extracting metadata for file: {FilePath}", filePath);
                var mediaInfo = await FFProbe.AnalyseAsync(filePath, null, cancellationToken);
                
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
                
                return new MediaMetadata(
                    mediaInfo.Duration.TotalSeconds,
                    audioStream?.CodecName,
                    audioStream?.SampleRateHz
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract metadata for file: {FilePath}", filePath);
                return null;
            }
        }
    }
}
