
namespace Upload.Api.Infrastructure.FileStorage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorage> _logger;
        
        public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
        {
            _basePath = configuration.GetValue<string>("Storage:BasePath") ?? "./storage/uploads";
            _logger = logger;

            // Garante que o diretório base exista
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created base storage directory at {BasePath}", _basePath);
            }
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        {
            try
            {
                var mediaId = Guid.NewGuid();
                var extension = Path.GetExtension(fileName);
                var newFileName = $"{mediaId}{extension}";
                var fullPath = Path.Combine(_basePath, newFileName);

                using var fileStreamOutput = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                await fileStream.CopyToAsync(fileStreamOutput, ct);

                // pegar filepath completo
                var completePath = Path.GetFullPath(fullPath);

                _logger.LogInformation("Arquivo salvo: {completePath}", completePath);

                return completePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar arquivo {fileName}", fileName);
                throw;
            }
        }

        public Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                if (File.Exists(filePath) == false)
                    return Task.FromResult(false); // se não existir, retorna false


                File.Delete(filePath);
                _logger.LogInformation("Arquivo deletado: {filePath}", filePath);

                return Task.FromResult(true);
            }
            catch (Exception)
            {

                _logger.LogError("Erro ao deletar arquivo {filePath}", filePath);
                throw;
            }
        }

        public Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default)
        {
            return Task.FromResult(File.Exists(filePath));
        }


    }
}
