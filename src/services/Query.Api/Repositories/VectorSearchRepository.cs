using Dapper;
using Npgsql;
using Pgvector;
using Pgvector.Dapper;
using Query.Api.DTOs;

namespace Query.Api.Repositories
{
    public class VectorSearchRepository
    {
        private readonly string _connectionString;

        public VectorSearchRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres") 
                ?? throw new ArgumentNullException("Connection string 'Postgres' not found.");
            
            SqlMapper.AddTypeHandler(new VectorTypeHandler());
        }

        public async Task<List<ResultSource>> SearchAsync(Vector queryEmbedding, string modelName, QueryFilters? filters, int topK)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    ts.media_id AS MediaId,
                    ts.text AS Text,
                    ts.start_seconds AS Start,
                    ts.end_seconds AS End,
                    e.embedding <=> @queryEmbedding AS Distance
                FROM embeddings e
                JOIN transcription_segments ts 
                    ON ts.id = e.transcription_segment_id
                WHERE e.model_name = @modelName
                  AND (@mediaIds IS NULL OR e.media_id = ANY(@mediaIds))
                  AND (@start IS NULL OR ts.start_seconds >= @start)
                  AND (@end IS NULL OR ts.end_seconds <= @end)
                ORDER BY Distance
                LIMIT @topK";

            var parameters = new
            {
                queryEmbedding,
                modelName,
                mediaIds = filters?.MediaIds?.Any() == true ? filters.MediaIds.ToArray() : null,
                start = filters?.StartSeconds,
                end = filters?.EndSeconds,
                topK
            };

            var results = await conn.QueryAsync<ResultSource>(sql, parameters);
            return results.ToList();
        }
    }
}
