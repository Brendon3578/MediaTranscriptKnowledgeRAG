using Microsoft.EntityFrameworkCore;
using Upload.Api.Domain;

namespace Upload.Api.Infrastructure.Persistence
{
    public class UploadDbContext : DbContext
    {
        public UploadDbContext(DbContextOptions<UploadDbContext> options)
            : base(options) { }

        public DbSet<MediaEntity> Media => Set<MediaEntity>();
        public DbSet<TranscriptionEntity> Transcriptions => Set<TranscriptionEntity>();
        public DbSet<TranscriptionSegmentEntity> TranscriptionSegments => Set<TranscriptionSegmentEntity>();
        public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaEntity>(e =>
            {
                e.ToTable("media");
                e.HasKey(e => e.Id);

                e.Property(e => e.FileName)
                    .IsRequired();

                e.Property(e => e.FilePath)
                    .IsRequired();

                e.Property(e => e.ContentType)
                    .IsRequired();

                e.Property(e => e.Status)
                    .HasConversion<int>();

                e.Property(e => e.DurationSeconds);
                e.Property(e => e.AudioCodec);
                e.Property(e => e.SampleRate);

                e.Property(e => e.TranscriptionProgressPercent);
                e.Property(e => e.EmbeddingProgressPercent);
                e.Property(e => e.TotalDurationSeconds);
                e.Property(e => e.ProcessedSeconds);

                e.HasIndex(e => e.CreatedAt);
                e.HasIndex(e => e.Status);

                e.HasOne(m => m.Transcription)
                    .WithOne(t => t.Media)
                    .HasForeignKey<TranscriptionEntity>(t => t.MediaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TranscriptionEntity>(e =>
            {
                e.ToTable("transcriptions");
                e.HasKey(e => e.Id);
                
                e.Property(e => e.Text).IsRequired();
            });

            modelBuilder.Entity<TranscriptionSegmentEntity>(e =>
            {
                e.ToTable("transcription_segments");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.StartSeconds).HasColumnType("real");
                e.Property(x => x.EndSeconds).HasColumnType("real");
                e.Property(x => x.Confidence).HasColumnType("real");
                e.Property(x => x.CreatedAt);
                e.HasIndex(x => x.MediaId);
                e.HasIndex(x => x.TranscriptionId);
                e.HasIndex(x => new { x.MediaId, x.SegmentIndex });
            });

            modelBuilder.Entity<EmbeddingEntity>(e =>
            {
                e.ToTable("embeddings");
                e.HasKey(x => x.Id);
                e.Property(x => x.MediaId).IsRequired();
            });
        }
    }
}
