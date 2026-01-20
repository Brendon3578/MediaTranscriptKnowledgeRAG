using MediaEmbedding.Worker.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaEmbedding.Worker.Infrastructure
{
    public class EmbeddingDbContext : DbContext
    {
        public EmbeddingDbContext(DbContextOptions<EmbeddingDbContext> options)
            : base(options) { }

        public DbSet<Transcription> Transcriptions => Set<Transcription>();
        public DbSet<Embedding> Embeddings => Set<Embedding>();
        public DbSet<TranscriptionSegment> TranscriptionSegments => Set<TranscriptionSegment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transcription>(e =>
            {
                e.ToTable("transcriptions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.Language);
                e.Property(x => x.CreatedAt);
            });

            modelBuilder.Entity<Embedding>(e =>
            {
                e.ToTable("embeddings");
                e.HasKey(x => x.Id);
                e.Property(x => x.EmbeddingVector)
                 .HasColumnType("vector(1536)");
                e.Property(x => x.CreatedAt);
            });

            modelBuilder.Entity<TranscriptionSegment>(e =>
            {
                e.ToTable("transcription_segments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.StartSeconds).HasColumnType("real");
                e.Property(x => x.EndSeconds).HasColumnType("real");
                e.Property(x => x.Confidence).HasColumnType("real");
                e.Property(x => x.CreatedAt);
                e.HasOne<Transcription>()
                    .WithMany()
                    .HasForeignKey(x => x.TranscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.MediaId);
                e.HasIndex(x => x.TranscriptionId);
                e.HasIndex(x => new { x.MediaId, x.SegmentIndex });
            });
        }
    }
}
