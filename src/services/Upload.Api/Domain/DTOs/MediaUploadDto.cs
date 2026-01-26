namespace Upload.Api.Domain.DTOs
{
    public class MediaUploadDto
    {
        public Guid MediaId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
