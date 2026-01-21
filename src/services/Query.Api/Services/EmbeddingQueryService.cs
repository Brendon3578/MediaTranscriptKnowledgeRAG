using Microsoft.Extensions.AI;
using Pgvector;

namespace Query.Api.Services
{
    public class EmbeddingQueryService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

        public EmbeddingQueryService(IEmbeddingGenerator<string, Embedding<float>> generator)
        {
            _generator = generator;
        }

        public async Task<Vector> GenerateQueryEmbeddingAsync(string question)
        {
            var result = await _generator.GenerateAsync(new[] { question });
            var embedding = result.FirstOrDefault();

            if (embedding == null)
            {
                throw new InvalidOperationException("Failed to generate embedding for the query.");
            }

            return new Vector(embedding.Vector.ToArray());
        }
    }
}
