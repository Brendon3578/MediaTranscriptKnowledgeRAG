namespace Upload.Api.Domain.DTOs
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
