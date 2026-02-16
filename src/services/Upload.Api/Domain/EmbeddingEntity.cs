using System.ComponentModel.DataAnnotations.Schema;

namespace Upload.Api.Domain
{
    [Table("embeddings")]
    public class EmbeddingEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }
    }
}
