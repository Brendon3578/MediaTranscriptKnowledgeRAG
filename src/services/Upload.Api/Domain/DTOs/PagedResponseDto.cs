namespace Upload.Api.Domain.DTOs
{
    public class PagedResponseDto<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long Total { get; set; }
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
    }
}
