using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Domain;
using Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace MediaTranscription.Worker.Infrastructure.Persistence
{
    public class TranscriptionRepository
    {
        private readonly TranscriptionDbContext _db;

        public TranscriptionRepository(TranscriptionDbContext context)
        {
            _db = context;
        }

        public async Task RemoveExistingTranscriptionByMediaId(Guid mediaId, CancellationToken cancellationToken)
        {
            var existingTranscription = await _db.Transcriptions
                .FirstOrDefaultAsync(t => t.MediaId == mediaId, cancellationToken);

            if (existingTranscription != null)
            {
                _db.Transcriptions.Remove(existingTranscription);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateMediaMetadataAsync(Guid mediaId, double? durationSeconds, string? audioCodec, int? sampleRate, CancellationToken cancellationToken)
        {
            var media = await _db.Media.FindAsync(new object[] { mediaId }, cancellationToken);
            if (media != null)
            {
                media.DurationSeconds = (float?)durationSeconds;
                media.TotalDurationSeconds = (float?)durationSeconds;
                media.AudioCodec = audioCodec;
                media.SampleRate = sampleRate;
                media.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<MediaEntity?> GetMediaByIdAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            return await _db.Media.FindAsync(new object[] { mediaId }, cancellationToken);
        }

        public async Task UpdateMediaAsync(MediaEntity media, CancellationToken cancellationToken)
        {
            media.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> CreateInitialTranscriptionAsync(Guid mediaId, string modelName, CancellationToken cancellationToken)
        {
            var transcription = new TranscriptionEntity
            {
                Id = Guid.NewGuid(),
                MediaId = mediaId,
                Text = string.Empty,
                Language = "auto",
                ModelName = modelName,
                ProcessingTimeSeconds = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transcriptions.Add(transcription);
            await _db.SaveChangesAsync(cancellationToken);

            return transcription.Id;
        }

        public async Task AddTranscriptionSegmentAsync(
            Guid transcriptionId, 
            Guid mediaId, 
            TranscriptionSegmentDto segmentDto, 
            CancellationToken cancellationToken)
        {
            // 1. Persistir segmento
            var segment = new TranscriptionSegmentEntity
            {
                Id = Guid.NewGuid(),
                TranscriptionId = transcriptionId,
                MediaId = mediaId,
                SegmentIndex = segmentDto.SegmentIndex,
                Text = segmentDto.Text,
                StartSeconds = (float)segmentDto.StartSeconds,
                EndSeconds = (float)segmentDto.EndSeconds,
                CreatedAt = DateTime.UtcNow
            };

            _db.TranscriptionSegments.Add(segment);

            // 2. Atualizar progresso na MediaEntity
            var media = await _db.Media.FindAsync(new object[] { mediaId }, cancellationToken);
            if (media != null)
            {
                media.ProcessedSeconds = (float)segmentDto.EndSeconds;
                
                if (media.TotalDurationSeconds.HasValue && media.TotalDurationSeconds.Value > 0)
                {
                    var progress = (media.ProcessedSeconds.Value / media.TotalDurationSeconds.Value) * 100;
                    media.TranscriptionProgressPercent = Math.Min(100f, progress);
                }
                
                media.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task FinalizeTranscriptionAsync(
            Guid transcriptionId, 
            Guid mediaId,
            string fullText, 
            string language, 
            double processingTime, 
            CancellationToken cancellationToken)
        {
            var transcription = await _db.Transcriptions.FindAsync(new object[] { transcriptionId }, cancellationToken);
            if (transcription != null)
            {
                transcription.Text = fullText;
                transcription.Language = language;
                transcription.ProcessingTimeSeconds = processingTime;
            }

            var media = await _db.Media.FindAsync(new object[] { mediaId }, cancellationToken);
            if (media != null)
            {
                media.Status = MediaStatus.TranscriptionCompleted;
                media.TranscriptionProgressPercent = 100;
                media.ProcessedSeconds = media.TotalDurationSeconds;
                media.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

    }
}
