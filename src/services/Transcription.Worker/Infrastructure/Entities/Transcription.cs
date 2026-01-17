using System.ComponentModel.DataAnnotations.Schema;

namespace MediaTranscription.Worker.Infrastructure.Entities
{
    public class Transcription
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("media_id")]
        public Guid MediaId { get; set; }

        [Column("text")]
        public string Text { get; set; } = null!;

        [Column("language")]
        public string? Language { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
