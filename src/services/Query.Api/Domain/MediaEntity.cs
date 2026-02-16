using System.ComponentModel.DataAnnotations.Schema;

namespace Query.Api.Domain
{
    public enum MediaStatus
    {
        Uploaded = 0,
        TranscriptionProcessing = 1,
        TranscriptionCompleted = 2,
        EmbeddingProcessing = 3,
        Completed = 4,
        Failed = 5
    }

    [Table("media")]
    public class MediaEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("status")]
        public MediaStatus Status { get; set; }

        [Column("transcription_progress_percent")]
        public float? TranscriptionProgressPercent { get; set; }

        [Column("embedding_progress_percent")]
        public float? EmbeddingProgressPercent { get; set; }

        [Column("total_duration_seconds")]
        public float? TotalDurationSeconds { get; set; }

        [Column("processed_seconds")]
        public float? ProcessedSeconds { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
