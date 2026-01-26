using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Domain;
using Shared.Contracts.Events;

namespace MediaTranscription.Worker.Infrastructure.Persistence
{
    public class TranscriptionRepository
    {
        private readonly TranscriptionDbContext _db;

        public TranscriptionRepository(TranscriptionDbContext context)
        {
            _db = context;
        }

        public async Task RemoveExistingTranscriptionSegmentsByMediaId(Guid mediaId, CancellationToken cancellationToken)
        {
            var existingSegments = _db.TranscriptionSegments
                .Where(s => s.MediaId == mediaId);

            _db.TranscriptionSegments.RemoveRange(existingSegments);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> SaveTranscriptionAndSegments(TranscriptionResultDto resultDto, MediaUploadedEvent mediaEvent, CancellationToken cancellationToken)
        {
            var transcriptionId = Guid.NewGuid();

            var transcription = new TranscriptionEntity
            {
                Id = transcriptionId,
                MediaId = mediaEvent.MediaId,
                Text = string.Empty,
                Language = resultDto.Language,
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
