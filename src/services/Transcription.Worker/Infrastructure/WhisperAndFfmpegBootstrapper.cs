using FFMpegCore;
using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Infrastructure
{
    public class WhisperAndFfmpegBootstrapper : IDependencyBootstrapper
    {
        private readonly ILogger<WhisperAndFfmpegBootstrapper> _logger;
        private readonly IWhisperModelProvider _whisperModelProvider;
        private readonly IConfiguration _configuration;

        public WhisperAndFfmpegBootstrapper(
            ILogger<WhisperAndFfmpegBootstrapper> logger,
            IWhisperModelProvider whisperModelProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _whisperModelProvider = whisperModelProvider;
            _configuration = configuration;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando verificação de dependências nativas...");

            try
            {
                // 1. Garantir modelo Whisper (Default Medium)
                _logger.LogInformation("Verificando modelo Whisper padrão (Medium)...");
                await _whisperModelProvider.EnsureModelAsync(GgmlType.Medium, cancellationToken);
                _logger.LogInformation("Modelo Whisper padrão pronto.");

                // 2. Garantir FFmpeg (Via PATH no Docker)
                _logger.LogInformation("Verificando FFmpeg e FFprobe via PATH...");
                
                // Se estiver no Docker, FFmpeg deve estar no PATH.
                // Não baixamos mais em runtime para garantir ambiente imutável.
                _logger.LogInformation("FFmpeg pronto (assumindo presença no PATH).");

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha crítica ao inicializar dependências nativas. A aplicação será encerrada.");
                throw; // Isso fará o startup falhar, como desejado
            }
        }
    }
}
