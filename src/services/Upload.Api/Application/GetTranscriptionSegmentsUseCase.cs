using Microsoft.EntityFrameworkCore;
using Upload.Api.Domain;
using Upload.Api.Infrastructure.Persistence;

namespace Upload.Api.Application
{
    public class GetTranscriptionSegmentsUseCase
    {
        private readonly UploadDbContext _dbContext;

        public GetTranscriptionSegmentsUseCase(UploadDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TranscriptionSegmentEntity>> GetSegmentsAfterIndex(
            Guid mediaId,
            int afterIndex,
            CancellationToken ct)
        {
            return await _dbContext.TranscriptionSegments
                .AsNoTracking()
                .Where(s => s.MediaId == mediaId && s.SegmentIndex > afterIndex)
                .OrderBy(s => s.SegmentIndex)
                .ToListAsync(ct);
        }
    }
}
