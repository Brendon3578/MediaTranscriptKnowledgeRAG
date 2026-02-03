using System.ComponentModel.DataAnnotations.Schema;

namespace MediaEmbedding.Worker.Domain.Models
{
    public enum MediaStatus
    {
        Uploaded = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }

    [Table("media")]
    public class MediaEntity
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("status")]
        public MediaStatus Status { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
