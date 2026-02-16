using Microsoft.EntityFrameworkCore;
using Upload.Api.Domain.DTOs;
using Upload.Api.Infrastructure.Persistence;

namespace Upload.Api.Application
{
    public class GetMediaStatusUseCase
    {
        private readonly UploadDbContext _dbContext;

        public GetMediaStatusUseCase(UploadDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MediaStatusDto?> ExecuteAsync(Guid mediaId, CancellationToken ct)
        {
            var media = await _dbContext.Media
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == mediaId, ct);

            if (media == null) return null;

            return new MediaStatusDto
            {
                MediaId = media.Id,
                Status = media.Status.ToString(),
                TranscriptionProgress = media.TranscriptionProgressPercent ?? 0,
                EmbeddingProgress = media.EmbeddingProgressPercent ?? 0
            };
        }
    }
}
