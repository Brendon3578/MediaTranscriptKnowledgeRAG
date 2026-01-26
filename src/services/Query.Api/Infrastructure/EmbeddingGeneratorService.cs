using Microsoft.Extensions.AI;
using Pgvector;

namespace Query.Api.Infrastructure
{
    public class EmbeddingGeneratorService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

        public EmbeddingGeneratorService(IEmbeddingGenerator<string, Embedding<float>> generator)
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
