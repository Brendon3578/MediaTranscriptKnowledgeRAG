using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace MediaEmbedding.Worker.Models
{
    [Table("embeddings")]
    public class EmbeddingEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }

        [Column("transcription_id")]
        public Guid TranscriptionId { get; set; }

        [Column("transcription_segment_id")]
        public Guid TranscriptionSegmentId { get; set; }

        [Column("model_name")]
        public string ModelName { get; set; } = null!;

        [Column("chunk_text")]
        public string ChunkText { get; set; } = null!;

        [Column("embedding", TypeName = "vector(1024)")]
        public Vector? EmbeddingVector { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
