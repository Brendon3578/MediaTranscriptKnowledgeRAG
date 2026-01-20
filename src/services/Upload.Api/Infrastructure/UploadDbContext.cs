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
        }
    }
}
