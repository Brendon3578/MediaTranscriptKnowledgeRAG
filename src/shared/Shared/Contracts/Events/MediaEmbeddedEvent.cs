namespace Shared.Contracts.Events
{
    public record MediaEmbeddedEvent
    {
        public Guid MediaId { get; init; }
        public string ModelName { get; init; } = string.Empty;
        public int ChunksCount { get; init; }
        public DateTime EmbeddedAt { get; init; }
    }
}
