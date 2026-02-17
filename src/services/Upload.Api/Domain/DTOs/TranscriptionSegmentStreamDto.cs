namespace Upload.Api.Domain.DTOs
{
    public sealed class TranscriptionSegmentStreamDto
    {
        public Guid MediaId { get; init; }
        public int Index { get; init; }
        public double StartSeconds { get; init; }
        public double EndSeconds { get; init; }
        public string Text { get; init; } = string.Empty;
    }
}
