using System.ComponentModel.DataAnnotations.Schema;

namespace Upload.Api.Domain
{
    public enum MediaStatus
    {
        Uploaded = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }

    public class MediaEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("file_name")]

        public string FileName { get; set; } = string.Empty;
        
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [Column("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [Column("status")]
        public MediaStatus Status { get; set; } = MediaStatus.Uploaded;

        [Column("file_size_bytes")]
        public long FileSizeBytes { get; set; }

        [Column("duration_seconds")]
        public float? DurationSeconds { get; set; }

        [Column("audio_codec")]
        public string? AudioCodec { get; set; }

        [Column("sample_rate")]
        public int? SampleRate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public virtual TranscriptionEntity? Transcription { get; set; }
    }
}
