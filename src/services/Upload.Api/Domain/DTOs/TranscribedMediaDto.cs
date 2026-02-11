namespace Upload.Api.Domain.DTOs
{
    public class TranscribedMediaDto
    {
        public Guid MediaId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty; // audio | video
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TranscriptionText { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
