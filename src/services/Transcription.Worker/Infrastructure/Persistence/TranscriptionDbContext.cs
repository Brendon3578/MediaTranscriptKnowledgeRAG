using MediaTranscription.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace MediaTranscription.Worker.Infrastructure.Persistence
{
    public class TranscriptionDbContext : DbContext
    {
        public TranscriptionDbContext(DbContextOptions<TranscriptionDbContext> options)
            : base(options) { }

        public DbSet<MediaEntity> Media => Set<MediaEntity>();
        public DbSet<TranscriptionEntity> Transcriptions => Set<TranscriptionEntity>();
        public DbSet<TranscriptionSegmentEntity> TranscriptionSegments => Set<TranscriptionSegmentEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaEntity>(e =>
            {
                e.ToTable("media");
                e.HasKey(e => e.Id);

                e.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(500);

                e.Property(e => e.FilePath)
                    .IsRequired()
                    .HasMaxLength(1000);

                e.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(e => e.Status)
                    .HasConversion<int>();

                e.HasIndex(e => e.CreatedAt);
                e.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<TranscriptionEntity>(e =>
            {
                e.ToTable("transcriptions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.Language);
                e.Property(x => x.CreatedAt);
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
                e.HasOne<TranscriptionEntity>()
                    .WithMany()
                    .HasForeignKey(x => x.TranscriptionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne<MediaEntity>()
                    .WithMany()
                    .HasForeignKey(x => x.MediaId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.MediaId);
                e.HasIndex(x => x.TranscriptionId);
                e.HasIndex(x => new { x.MediaId, x.SegmentIndex });
            });

        }
    }
}
