namespace Query.Api.Application
{
    public interface IOllamaHealthCheckService
    {
        Task CheckAvailabilityAsync(CancellationToken cancellationToken);
    }
}

