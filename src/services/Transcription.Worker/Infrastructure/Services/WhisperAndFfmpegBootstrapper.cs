using FFMpegCore;
using MediaTranscription.Worker.Configuration;
using MediaTranscription.Worker.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xabe.FFmpeg.Downloader;

namespace MediaTranscription.Worker.Infrastructure.Services
{
    public class WhisperAndFfmpegBootstrapper : IDependencyBootstrapper
    {
        private readonly ILogger<WhisperAndFfmpegBootstrapper> _logger;
        private readonly IWhisperModelProvider _whisperModelProvider;
        private readonly FFmpegOptions _ffmpegOptions;

        public WhisperAndFfmpegBootstrapper(
            ILogger<WhisperAndFfmpegBootstrapper> logger,
            IWhisperModelProvider whisperModelProvider,
            IOptions<FFmpegOptions> ffmpegOptions)
        {
            _logger = logger;
            _whisperModelProvider = whisperModelProvider;
            _ffmpegOptions = ffmpegOptions.Value;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando verificação de dependências nativas...");

            try
            {
                // 1. Garantir modelo Whisper
                _logger.LogInformation("Verificando modelo Whisper...");
                await _whisperModelProvider.EnsureModelAsync(cancellationToken);
                _logger.LogInformation("Modelo Whisper pronto.");

                // 2. Garantir FFmpeg
                _logger.LogInformation("Verificando FFmpeg...");
                var ffmpegDir = Path.Combine(AppContext.BaseDirectory, _ffmpegOptions.ExecutablePath);
                if (!Directory.Exists(ffmpegDir))
                {
                    Directory.CreateDirectory(ffmpegDir);
                }

                // Configura Xabe para baixar na pasta específica
                // Nota: O método DownloadFFmpegSuite baixa para o diretório especificado ou atual
                // O método GetLatestVersion do Xabe baixa para o diretório especificado.
                
                // Verificamos se já existe o executável para evitar download desnecessário
                // O Xabe geralmente baixa ffmpeg.exe e ffprobe.exe
                var ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");
                
                if (!File.Exists(ffmpegExe))
                {
                    _logger.LogInformation("Baixando FFmpeg para {FFmpegDir}...", ffmpegDir);
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegDir);
                    _logger.LogInformation("Download do FFmpeg concluído.");
                }
                else
                {
                    _logger.LogInformation("FFmpeg já existe em {FFmpegPath}.", ffmpegExe);
                }

                // Configura FFMpegCore para usar esse diretório
                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });
                
                _logger.LogInformation("FFmpeg pronto e configurado.");

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Falha crítica ao inicializar dependências nativas. A aplicação será encerrada.");
                throw; // Isso fará o startup falhar, como desejado
            }
        }
    }
}
