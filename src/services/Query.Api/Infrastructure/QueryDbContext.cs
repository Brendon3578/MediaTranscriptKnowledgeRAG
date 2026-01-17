using Microsoft.EntityFrameworkCore;
using Query.Api.Infrastructure.Entities;

namespace Query.Api.Infrastructure
{
    public class QueryDbContext  : DbContext
    {
        public QueryDbContext (DbContextOptions<QueryDbContext > options)
            : base(options) { }

        public DbSet<Embedding> Embeddings => Set<Embedding>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Embedding>(e =>
            {
                e.ToTable("embeddings");
                e.HasKey(x => x.Id);
                e.Property(x => x.EmbeddingVector)
                 .HasColumnType("vector(1536)");
                e.Property(x => x.CreatedAt);
            });
        }
    }
}
