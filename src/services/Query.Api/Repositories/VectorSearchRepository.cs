using System.Data;
using Dapper;
using Npgsql;
using Pgvector;
using Query.Api.DTOs;

namespace Query.Api.Repositories
{
    public sealed class VectorSearchRepository
    {
        private readonly string _connectionString;

        public VectorSearchRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new ArgumentNullException("Connection string 'Postgres' not found.");
        }

        public async Task<List<ResultSource>> SearchAsync(
            Vector queryEmbedding,
            string modelName,
            QueryFilters? filters,
            int topK)
        {
            const string sql = """
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
                LIMIT @topK
            """;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();


            await using var conn = await dataSource.OpenConnectionAsync();

            // 🔴 Parâmetro pgvector CRÍTICO
            var vectorParameter = new NpgsqlParameter
            {
                ParameterName = "queryEmbedding",
                Value = queryEmbedding,
                DataTypeName = "vector"
            };

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.Add(vectorParameter);

            cmd.Parameters.AddWithValue("modelName", modelName);
            cmd.Parameters.AddWithValue("topK", topK);

            cmd.Parameters.AddWithValue(
                "mediaIds",
                filters?.MediaIds?.Any() == true
                    ? filters.MediaIds.ToArray()
                    : DBNull.Value
            );

            cmd.Parameters.AddWithValue(
                "start",
                filters?.StartSeconds ?? (object)DBNull.Value
            );

            cmd.Parameters.AddWithValue(
                "end",
                filters?.EndSeconds ?? (object)DBNull.Value
            );

            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<ResultSource>();

            while (await reader.ReadAsync())
            {
                results.Add(new ResultSource
                {
                    MediaId = reader.GetGuid(reader.GetOrdinal("MediaId")),
                    Text = reader.GetString(reader.GetOrdinal("Text")),
                    Start = reader.GetFloat(reader.GetOrdinal("Start")),
                    End = reader.GetFloat(reader.GetOrdinal("End")),
                    Distance = reader.GetFloat(reader.GetOrdinal("Distance"))
                });
            }

            return results;
        }
    }
}
