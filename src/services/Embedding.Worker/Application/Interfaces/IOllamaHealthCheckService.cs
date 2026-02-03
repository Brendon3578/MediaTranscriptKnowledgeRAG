namespace MediaEmbedding.Worker.Application.Interfaces
{
    public interface IOllamaHealthCheckService
    {
        Task CheckAvailabilityAsync(CancellationToken cancellationToken);
    }
}

