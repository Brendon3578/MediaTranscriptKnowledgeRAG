using System.ComponentModel.DataAnnotations.Schema;
using Upload.Api.Infrastructure.Enum;

namespace Upload.Api.Infrastructure.Entities
{
    public class Media
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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
