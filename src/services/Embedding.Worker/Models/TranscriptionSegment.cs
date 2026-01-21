using System.ComponentModel.DataAnnotations.Schema;

namespace MediaEmbedding.Worker.Models
{
    [Table("transcription_segments")]
    public class TranscriptionSegment
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("transcription_id")]
        public Guid TranscriptionId { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }

        [Column("segment_index")]
        public int SegmentIndex { get; set; }

        [Column("text")]
        public string Text { get; set; } = null!;

        [Column("start_seconds")]
        public float StartSeconds { get; set; }

        [Column("end_seconds")]
        public float EndSeconds { get; set; }

        [Column("confidence")]
        public float? Confidence { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
