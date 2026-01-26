namespace Upload.Api.Application.Interfaces
{
    public interface IFileStorageFacade
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
        Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default);
        Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default);
    }
}
