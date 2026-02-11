using System.Data;
using Dapper;
using Npgsql;
using Pgvector;
using Query.Api.Application;
using System.Text.Json;

namespace Query.Api.Infrastructure.Persistence
{
    public sealed class TranscriptionSegmentVectorSearchRepository
    {
        private readonly string _connectionString;

        public TranscriptionSegmentVectorSearchRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new ArgumentNullException("Connection string 'Postgres' not found.");
        }

        public async Task<List<ResultSource>> SearchAsync(
            Vector queryEmbedding,
            string modelName,
            List<MediaTimeRange> timeRanges,
            int topK,
            float maxDistance)
        {
            const string sql = """
                WITH time_ranges AS ( 
                    SELECT * 
                    FROM jsonb_to_recordset(@timeRanges) AS r ( 
                        media_id uuid, 
                        start_seconds real, 
                        end_seconds real 
                    ) 
                ) 
                SELECT 
                    ts.media_id      AS MediaId, 
                    ts.text          AS Text, 
                    ts.start_seconds AS Start, 
                    ts.end_seconds   AS End, 
                    d.distance       AS Distance 
                FROM transcription_segments ts 
                JOIN time_ranges r 
                  ON ts.media_id = r.media_id 
                 AND ts.start_seconds = r.start_seconds 
                 AND ts.end_seconds   <= r.end_seconds 
                JOIN embeddings e 
                  ON e.transcription_segment_id = ts.id 
                 AND e.model_name = @modelName 
                CROSS JOIN LATERAL ( 
                    SELECT (e.embedding <=> @queryEmbedding) AS distance 
                ) d 
                WHERE d.distance < @maxDistance 
                ORDER BY d.distance 
                LIMIT @topK;
                """;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();

            await using var conn = await dataSource.OpenConnectionAsync();

            var timeRangesJson = JsonSerializer.Serialize(timeRanges, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
            });

            var parameters = new DynamicParameters();
            parameters.Add("queryEmbedding", queryEmbedding);
            parameters.Add("modelName", modelName);
            parameters.Add("timeRanges", timeRangesJson, DbType.Object); // Npgsql handles JSONB with DbType.Object if passed as string/json
            parameters.Add("topK", topK);
            parameters.Add("maxDistance", maxDistance);

            // However, Dapper with Npgsql and pgvector needs care.
            // The previous implementation used NpgsqlCommand directly for Vector parameter.
            // Dapper supports custom type handlers, but mixing NpgsqlCommand and Dapper is tricky if we need vector support explicitly.
            // The previous code used `NpgsqlCommand` and `cmd.Parameters.Add(vectorParameter)`.
            // The user says "Executes the query below using Dapper or raw SQL".
            // Since I need to pass Vector, and Dapper might not support Pgvector types out of the box without configuration,
            // I should stick to what was working or configure Dapper.
            // But the previous code used `NpgsqlCommand`.
            // The user says "Execute the query below using Dapper or raw SQL".
            
            // If I use Dapper, I need to register the vector type handler or use NpgsqlCommand.
            // The previous code used NpgsqlCommand explicitly for `queryEmbedding`.
            
            // Let's stick to `NpgsqlCommand` to be safe with `Vector` type, or use Dapper if I'm sure.
            // Dapper 2.0+ with Npgsql 6.0+ might need setup.
            // I'll stick to `NpgsqlCommand` to minimize risk, or adapt the Dapper usage if I can.
            
            // Wait, the previous code used `NpgsqlCommand`. I can just adapt it.
            // But the user mentioned Dapper.
            // "Executes the query below using Dapper or raw SQL (do not use EF Core for vector search)"
            
            // I'll use NpgsqlCommand because it handles the Vector type correctly in the existing code.
            // Dapper would require `SqlMapper.AddTypeHandler(new VectorTypeHandler());` or similar if not built-in.
            // Actually, the previous code did `dataSourceBuilder.UseVector()`.
            
            // Let's try to use Dapper if possible, but NpgsqlCommand is safer given I have the vector object.
            // I will use NpgsqlCommand as in the previous code but with the new query.
            
            // Re-reading: "Executes the query below using Dapper or raw SQL".
            // "Raw SQL" via NpgsqlCommand is fine.
            
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
            cmd.Parameters.AddWithValue("maxDistance", maxDistance);
            
            // For JSONB
            var jsonParam = new NpgsqlParameter("timeRanges", NpgsqlTypes.NpgsqlDbType.Jsonb)
            {
                Value = timeRangesJson
            };
            cmd.Parameters.Add(jsonParam);

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
