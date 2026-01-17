namespace Upload.Api.Messaging
{
    public record MediaUploadedEvent
    {
        public Guid MediaId { get; init; }
        public string FilePath { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public DateTime UploadedAt { get; init; }
    }
}
