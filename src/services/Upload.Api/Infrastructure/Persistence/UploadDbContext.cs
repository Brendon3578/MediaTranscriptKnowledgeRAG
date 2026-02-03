using Microsoft.EntityFrameworkCore;
using Upload.Api.Domain;

namespace Upload.Api.Infrastructure.Persistence
{
    public class UploadDbContext : DbContext
    {
        public UploadDbContext(DbContextOptions<UploadDbContext> options)
            : base(options) { }

        public DbSet<MediaEntity> Media => Set<MediaEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MediaEntity>(e =>
            {
                e.ToTable("media");
                e.HasKey(e => e.Id);

                e.Property(e => e.FileName)
                    .IsRequired()

                e.Property(e => e.FilePath)
                    .IsRequired()

                e.Property(e => e.ContentType)
                    .IsRequired()

                e.Property(e => e.Status)
                    .HasConversion<int>();

                e.HasIndex(e => e.CreatedAt);
                e.HasIndex(e => e.Status);
            });
        }
    }
}
