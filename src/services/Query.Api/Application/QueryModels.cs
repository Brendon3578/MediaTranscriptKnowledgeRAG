namespace Query.Api.Application
{
    public class QueryRequest
    {
        public string Question { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional: List of specific time ranges per media to restrict search.
        /// </summary>
        public List<MediaTimeRange>? TimeRanges { get; set; }

        public int TopK { get; set; } = 5;

        /// <summary>
        /// Maximum cosine distance (1 - cosine similarity) for relevance.
        /// Default is 0.5 if not specified.
        /// </summary>
        public float MaxDistance { get; set; } = 0.5f;

        /// <summary>
        /// Name of the embedding model to use.
        /// </summary>
        public string? ModelName { get; set; }
    }

    public class MediaTimeRange
    {
        public Guid MediaId { get; set; }
        public float StartSeconds { get; set; }
        public float EndSeconds { get; set; }
    }

    public class QueryResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<ResultSource> Sources { get; set; } = new();
    }

    public class ResultSource
    {
        public Guid MediaId { get; set; }
        public float Start { get; set; }
        public float End { get; set; }
        public string Text { get; set; } = string.Empty;
        public double Distance { get; set; }
    }

}
