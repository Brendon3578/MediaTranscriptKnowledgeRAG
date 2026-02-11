using Npgsql;
using NpgsqlTypes;
using Pgvector;
using Query.Api.Application;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Query.Api.Infrastructure.Persistence
{
    public sealed class TranscriptionSegmentVectorSearchRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public TranscriptionSegmentVectorSearchRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IReadOnlyList<ResultSource>> SearchAsync(
            Vector queryEmbedding,
            string modelName,
            IReadOnlyCollection<MediaTimeRange> timeRanges,
            float maxDistance,
            int topK)
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
                AND ts.start_seconds >= r.start_seconds
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

            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);

            // Vector Parameter
            cmd.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "queryEmbedding",
                Value = queryEmbedding,
                DataTypeName = "vector"
            });

            // Standard Parameters
            cmd.Parameters.AddWithValue("modelName", modelName);
            cmd.Parameters.AddWithValue("maxDistance", maxDistance);
            cmd.Parameters.AddWithValue("topK", topK);

            // JSONB Parameter
            var timeRangesJson = JsonSerializer.Serialize(timeRanges, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            cmd.Parameters.Add(new NpgsqlParameter("timeRanges", NpgsqlDbType.Jsonb)
            {
                Value = timeRangesJson
            });

            await using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<ResultSource>();

            while (await reader.ReadAsync())
            {
                results.Add(new ResultSource
                {
                    MediaId = reader.GetGuid(reader.GetOrdinal("MediaId")),
                    Text = reader.GetString(reader.GetOrdinal("Text")),
                    Start = reader.GetFloat(reader.GetOrdinal("Start")),
                    End = reader.GetFloat(reader.GetOrdinal("End")),
                    Distance = reader.GetDouble(reader.GetOrdinal("Distance"))
                });
            }

            return results;
        }
    }
}
