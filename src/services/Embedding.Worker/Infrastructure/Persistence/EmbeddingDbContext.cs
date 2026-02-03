using MediaEmbedding.Worker.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaEmbedding.Worker.Infrastructure.Persistence
{
    public class EmbeddingDbContext : DbContext
    {
        public EmbeddingDbContext(DbContextOptions<EmbeddingDbContext> options)
            : base(options) { }

        public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();
        public DbSet<TranscriptionSegment> TranscriptionSegments => Set<TranscriptionSegment>();
        public DbSet<MediaEntity> Media => Set<MediaEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

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

            modelBuilder.Entity<TranscriptionSegment>(e =>
            {
                e.ToTable("transcription_segments");
                e.HasKey(x => x.Id);
            });
        }
    }
}
