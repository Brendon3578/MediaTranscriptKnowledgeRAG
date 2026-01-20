using MediaTranscription.Worker.Facade;
using MediaTranscription.Worker.Infrastructure.Entities;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MediaTranscription.Worker.Infrastructure.Services
{
    public class TranscriptionDataService
    {
        private readonly TranscriptionDbContext _db;

        public TranscriptionDataService(TranscriptionDbContext context)
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

            var transcription = new Transcription
            {
                Id = transcriptionId,
                MediaId = mediaEvent.MediaId,
                Text = string.Empty,
                Language = resultDto.Language,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transcriptions.Add(transcription);

            var newSegments = resultDto.Segments.Select(s => new TranscriptionSegment
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
