namespace Query.Api.Application
{
    public class QueryRequest
    {
        public string Question { get; set; } = string.Empty;
        public QueryFilters? Filters { get; set; }
        public int TopK { get; set; } = 5;
    }

    public class QueryFilters
    {
        public List<Guid>? MediaIds { get; set; }
        public float? StartSeconds { get; set; }
        public float? EndSeconds { get; set; }
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
