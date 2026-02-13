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

        public async Task<Guid> SaveTranscriptionAndSegments(TranscriptionResultDto resultDto, MediaUploadedEvent mediaEvent, CancellationToken cancellationToken)
        {
            var transcriptionId = Guid.NewGuid();

            var transcription = new TranscriptionEntity
            {
                Id = transcriptionId,
                MediaId = mediaEvent.MediaId,
                Text = resultDto.TranscriptionText,
                Language = resultDto.Language,
                ModelName = resultDto.ModelName,
                ProcessingTimeSeconds = resultDto.ProcessingTimeSeconds,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transcriptions.Add(transcription);

            var newSegments = resultDto.Segments.Select(s => new TranscriptionSegmentEntity
            {
                Id = Guid.NewGuid(),
                TranscriptionId = transcriptionId,
                MediaId = mediaEvent.MediaId,
                SegmentIndex = s.SegmentIndex,
                Text = s.Text,
                StartSeconds = (float)s.StartSeconds,
                EndSeconds = (float)s.EndSeconds,
                Confidence = null,
                CreatedAt = DateTime.UtcNow
            });

            await _db.TranscriptionSegments.AddRangeAsync(newSegments, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return transcriptionId;
        }
    }
}
