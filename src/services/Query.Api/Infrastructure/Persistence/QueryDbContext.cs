using Microsoft.EntityFrameworkCore;
using Query.Api.Domain;

namespace Query.Api.Infrastructure.Persistence
{
    public class QueryDbContext  : DbContext
    {
        public QueryDbContext (DbContextOptions<QueryDbContext > options)
            : base(options) { }

        public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();
        public DbSet<MediaEntity> Media => Set<MediaEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaEntity>(e =>
            {
                e.ToTable("media");
                e.HasKey(e => e.Id);
                e.Property(e => e.Status).HasConversion<int>();
            });

            modelBuilder.Entity<EmbeddingEntity>(e =>
            {
                e.ToTable("embeddings");
                e.HasKey(x => x.Id);
                e.Property(x => x.ChunkText).IsRequired();
                e.Property(x => x.ModelName).IsRequired();
                e.Property(x => x.EmbeddingVector);
                e.Property(x => x.CreatedAt);
            });
        }
    }
}
