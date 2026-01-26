using Microsoft.Extensions.Logging;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Infrastructure
{
    public interface IWhisperModelProvider
    {
        Task<string> EnsureModelAsync(CancellationToken cancellationToken);
    }

    public class WhisperAIModelProvider : IWhisperModelProvider
    {
        private readonly ILogger<WhisperAIModelProvider> _logger;
        private readonly IConfiguration _configuration;

        public WhisperAIModelProvider(
            ILogger<WhisperAIModelProvider> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> EnsureModelAsync(CancellationToken cancellationToken)
        {
            var modelsDirectory = _configuration["Whisper:ModelsDirectory"] ?? "models";
            var modelsDir = Path.Combine(AppContext.BaseDirectory, modelsDirectory);
            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
                _logger.LogInformation("Diretório de modelos criado: {ModelsDir}", modelsDir);
            }

            // Nome do arquivo baseado no tipo (ex: ggml-base.bin)
            // A biblioteca geralmente usa convenção "ggml-{type}.bin"
            var modelTypeStr = _configuration["Whisper:ModelType"] ?? "Medium";

            if (!Enum.TryParse<GgmlType>(modelTypeStr, true, out var modelType))
            {
                modelType = GgmlType.Medium;
            }

            var modelFileName = $"ggml-{modelType.ToString().ToLower()}.bin";
            var modelPath = Path.Combine(modelsDir, modelFileName);

            if (File.Exists(modelPath))
            {
                _logger.LogInformation("Modelo Whisper já existe em: {ModelPath}", modelPath);
                return modelPath;
            }

            _logger.LogInformation("Iniciando download do modelo Whisper ({Type}) para {ModelPath}...", modelType, modelPath);

            try
            {
                using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType, cancellationToken: cancellationToken);
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
