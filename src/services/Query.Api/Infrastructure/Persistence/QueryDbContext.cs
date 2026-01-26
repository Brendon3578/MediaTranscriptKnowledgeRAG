using Microsoft.EntityFrameworkCore;
using Query.Api.Domain;

namespace Query.Api.Infrastructure.Persistence
{
    public class QueryDbContext  : DbContext
    {
        public QueryDbContext (DbContextOptions<QueryDbContext > options)
            : base(options) { }

        public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<EmbeddingEntity>(e =>
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
