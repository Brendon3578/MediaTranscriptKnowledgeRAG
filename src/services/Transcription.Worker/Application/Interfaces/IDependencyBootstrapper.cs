namespace MediaTranscription.Worker.Application.Interfaces
{
    public interface IDependencyBootstrapper
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
