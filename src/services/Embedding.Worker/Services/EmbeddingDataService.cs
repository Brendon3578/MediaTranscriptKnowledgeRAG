using MediaEmbedding.Worker.Data;
using MediaEmbedding.Worker.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MediaEmbedding.Worker.Services
{
    public class EmbeddingDataService
    {
        private readonly EmbeddingDbContext _db;

        public EmbeddingDataService(EmbeddingDbContext db)
        {
            _db = db;
        }

        public async Task<List<TranscriptionSegment>> FindTranscriptionSegmentsByMediaIdAsync(Guid mediaId)
        {
            return await _db.TranscriptionSegments
                .Where(s => s.MediaId == mediaId)
                .OrderBy(s => s.SegmentIndex)
                .ToListAsync();
        }

        public async Task<bool> EmbeddingExistsForSegmentAsync(Guid segmentId, string modelName)
        {
            return await _db.Embeddings.AnyAsync(e =>
                        e.TranscriptionSegmentId == segmentId &&
                        e.ModelName == modelName);
        }

        public async Task SaveEmbeddingAsync(EmbeddingEntity embedding)
        {
            await _db.Embeddings.AddAsync(embedding);
        }

        public async Task SaveEmbeddingsAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
