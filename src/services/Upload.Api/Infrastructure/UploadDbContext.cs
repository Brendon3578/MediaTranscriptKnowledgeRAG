using Microsoft.EntityFrameworkCore;
using Upload.Api.Infrastructure.Entities;

namespace Upload.Api.Infrastructure
{
    public class UploadDbContext : DbContext
    {
        public UploadDbContext(DbContextOptions<UploadDbContext> options)
            : base(options) { }

        public DbSet<Media> Media => Set<Media>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Media>(entity =>
            {
                entity.ToTable("media");
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
        }
    }
}
