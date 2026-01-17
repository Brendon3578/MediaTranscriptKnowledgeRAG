namespace Upload.Api.Infrastructure.FileStorage
{
    public interface IFileStorage
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
        Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default);
        Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default);
    }
}
