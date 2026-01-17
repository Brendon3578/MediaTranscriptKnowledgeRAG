namespace Upload.Api.Infrastructure.DTOs
{
    public record ErrorResponseRequest
    {
        public string Error { get; set; } = string.Empty;

        public ErrorResponseRequest(string error)
        {
            Error = error;
        }
    }
}
