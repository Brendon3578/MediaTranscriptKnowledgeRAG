namespace MediaTranscription.Worker.Interfaces
{
    public interface IDependencyBootstrapper
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
