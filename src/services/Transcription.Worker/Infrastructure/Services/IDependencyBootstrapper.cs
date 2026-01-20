
namespace MediaTranscription.Worker.Infrastructure.Services
{
    public interface IDependencyBootstrapper
    {
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}
