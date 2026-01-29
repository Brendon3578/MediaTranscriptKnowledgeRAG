using MediaTranscription.Worker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Infrastructure
{
    public class WhisperAIModelProvider : IWhisperModelProvider
    {
        private readonly ILogger<WhisperAIModelProvider> _logger;
        private readonly IConfiguration _configuration;

        public WhisperAIModelProvider(
            ILogger<WhisperAIModelProvider> logger,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> EnsureModelAsync(GgmlType modelType, CancellationToken cancellationToken)
        {
            var modelName = modelType.ToString();

            var modelsDirectory = _configuration["Whisper:ModelsDirectory"] ?? "models";
            var modelsDir = Path.Combine(AppContext.BaseDirectory, modelsDirectory);
            
            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
            }

            var modelPath = Path.Combine(modelsDir, $"ggml-{modelName.ToLower()}.bin");

            if (File.Exists(modelPath))
            {
                _logger.LogInformation("Modelo Whisper {ModelName} encontrado em {ModelPath}", modelName, modelPath);
                return modelPath;
            }

            _logger.LogInformation("Modelo Whisper {ModelName} não encontrado. Iniciando download...", modelName);

            try
            {
                using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType, cancellationToken: cancellationToken);
                using var fileStream = File.Create(modelPath);
                await modelStream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation("Download do modelo {ModelName} concluído com sucesso.", modelName);
                return modelPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar o modelo Whisper {ModelName}", modelName);
                if (File.Exists(modelPath))
                {
                    File.Delete(modelPath);
                }
                throw;
            }
        }
    }
}
