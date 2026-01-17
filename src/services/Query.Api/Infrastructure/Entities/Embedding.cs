using System.ComponentModel.DataAnnotations.Schema;

namespace Query.Api.Infrastructure.Entities
{
    public class Embedding
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }

        [Column("transcription_id")]
        public Guid TranscriptionId { get; set; }

        [Column("chunk_text")]
        public string ChunkText { get; set; } = null!;

        [Column("embedding")]
        public float[] EmbeddingVector { get; set; } = Array.Empty<float>();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
