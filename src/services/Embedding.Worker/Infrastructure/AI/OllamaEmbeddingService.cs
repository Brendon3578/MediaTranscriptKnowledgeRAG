using MediaEmbedding.Worker.Application.Interfaces;
using Microsoft.Extensions.AI;

namespace MediaEmbedding.Worker.Infrastructure.AI
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

        public OllamaEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
        {
            _generator = generator;
        }

        public async Task<float[]> GenerateAsync(string text, CancellationToken ct)
        {
            var result = await _generator.GenerateAsync(new[] { text }, cancellationToken: ct);

            var embedding = result.FirstOrDefault();
            
            if (embedding == null)
            {
                throw new InvalidOperationException("Failed to generate embedding: No result returned.");
            }

            return embedding.Vector.ToArray();
        }
    }
}
