namespace MediaEmbedding.Worker.Application.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateAsync(string text, CancellationToken ct);
    }
}
