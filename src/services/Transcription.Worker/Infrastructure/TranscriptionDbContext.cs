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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Media>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FilePath)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Status)
                    .HasConversion<int>();

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<Transcription>(e =>
            {
                e.ToTable("transcriptions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Text).IsRequired();
                e.Property(x => x.Language);
                e.Property(x => x.CreatedAt);
            });

        }
    }
}
