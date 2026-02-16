using Microsoft.EntityFrameworkCore;
using Query.Api.Domain;
using Query.Api.Infrastructure.Persistence;

namespace Query.Api.Application
{
    public class GetMediaStatusUseCase
    {
        private readonly QueryDbContext _db;

        public GetMediaStatusUseCase(QueryDbContext db)
        {
            _db = db;
        }

        public async Task<MediaStatusResponse?> ExecuteAsync(Guid mediaId)
        {
            var media = await _db.Media
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == mediaId);

            if (media == null) return null;

            return new MediaStatusResponse
            {
                MediaId = media.Id,
                Status = media.Status.ToString(),
                TranscriptionProgress = media.TranscriptionProgressPercent ?? 0,
                EmbeddingProgress = media.EmbeddingProgressPercent ?? 0
            };
        }
    }
}
