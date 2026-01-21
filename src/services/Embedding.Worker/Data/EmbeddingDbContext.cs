using MediaEmbedding.Worker.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaEmbedding.Worker.Data
{
    public class EmbeddingDbContext : DbContext
    {
        public EmbeddingDbContext(DbContextOptions<EmbeddingDbContext> options)
            : base(options) { }

        public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();
        public DbSet<TranscriptionSegment> TranscriptionSegments => Set<TranscriptionSegment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<EmbeddingEntity>(e =>
            {
                e.ToTable("embeddings");
                e.HasKey(x => x.Id);
                e.Property(x => x.ChunkText).IsRequired();
                e.Property(x => x.ModelName).IsRequired();
                e.Property(x => x.EmbeddingVector).HasColumnType("vector(768)");
                e.Property(x => x.CreatedAt);
                
                // Idempotency index
                e.HasIndex(x => new { x.TranscriptionSegmentId, x.ModelName }).IsUnique();
            });

            modelBuilder.Entity<TranscriptionSegment>(e =>
            {
                e.ToTable("transcription_segments");
                e.HasKey(x => x.Id);
                // Read-only access essentially, but we need to map it
            });
        }
    }
}
