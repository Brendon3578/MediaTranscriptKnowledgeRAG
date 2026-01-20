using MediaTranscription.Worker.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Infrastructure.Services
{
    public interface IWhisperModelProvider
    {
        Task<string> EnsureModelAsync(CancellationToken cancellationToken);
    }

    public class WhisperGgmlModelProvider : IWhisperModelProvider
    {
        private readonly ILogger<WhisperGgmlModelProvider> _logger;
        private readonly WhisperOptions _options;

        public WhisperGgmlModelProvider(
            ILogger<WhisperGgmlModelProvider> logger,
            IOptions<WhisperOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task<string> EnsureModelAsync(CancellationToken cancellationToken)
        {
            var modelsDir = Path.Combine(AppContext.BaseDirectory, _options.ModelsDirectory);
            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
                _logger.LogInformation("Diretório de modelos criado: {ModelsDir}", modelsDir);
            }

            // Nome do arquivo baseado no tipo (ex: ggml-base.bin)
            // A biblioteca geralmente usa convenção "ggml-{type}.bin"
            var modelFileName = $"ggml-{_options.ModelType.ToString().ToLower()}.bin";
            var modelPath = Path.Combine(modelsDir, modelFileName);

            if (File.Exists(modelPath))
            {
                _logger.LogInformation("Modelo Whisper já existe em: {ModelPath}", modelPath);
                return modelPath;
            }

            _logger.LogInformation("Iniciando download do modelo Whisper ({Type}) para {ModelPath}...", _options.ModelType, modelPath);

            try
            {
                using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(_options.ModelType, cancellationToken: cancellationToken);
                using var fileStream = File.Create(modelPath);

                await modelStream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation("Download do modelo concluído com sucesso.");
                return modelPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao baixar modelo Whisper.");
                // Se falhar e ficar um arquivo corrompido/vazio, removemos
                if (File.Exists(modelPath))
                {
                    try { File.Delete(modelPath); } catch { }
                }
                throw;
            }
        }
    }
}
