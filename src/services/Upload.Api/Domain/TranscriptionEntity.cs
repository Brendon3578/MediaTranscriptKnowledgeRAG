using System.ComponentModel.DataAnnotations.Schema;

namespace Upload.Api.Domain
{
    [Table("transcriptions")]
    public class TranscriptionEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }

        [Column("text")]
        public string Text { get; set; } = string.Empty;

        [Column("language")]
        public string? Language { get; set; }

        [Column("model_name")]
        public string? ModelName { get; set; }

        [Column("processing_time_seconds")]
        public float? ProcessingTimeSeconds { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public MediaEntity? Media { get; set; }
    }
}
