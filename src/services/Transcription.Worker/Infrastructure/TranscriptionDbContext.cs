using MediaTranscription.Worker.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaTranscription.Worker.Infrastructure
{
    public class TranscriptionDbContext : DbContext
    {
        public TranscriptionDbContext(DbContextOptions<TranscriptionDbContext> options)
            : base(options) { }

        public DbSet<Media> Media => Set<Media>();
        public DbSet<Transcription> Transcriptions => Set<Transcription>();
        public DbSet<TranscriptionSegment> TranscriptionSegments => Set<TranscriptionSegment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Media>(e =>
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

            modelBuilder.Entity<Transcription>(e =>
            {
                e.ToTable("transcriptions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.Language);
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
                e.HasOne<Media>()
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
