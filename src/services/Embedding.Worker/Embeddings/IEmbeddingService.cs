namespace MediaEmbedding.Worker.Embeddings
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateAsync(string text, CancellationToken ct);
    }
}
