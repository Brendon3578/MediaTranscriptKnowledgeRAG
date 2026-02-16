namespace Upload.Api.Domain.DTOs
{
    public class MediaStatusDto
    {
        public Guid MediaId { get; set; }
        public string Status { get; set; } = string.Empty;
        public float TranscriptionProgress { get; set; }
        public float EmbeddingProgress { get; set; }
    }
}
