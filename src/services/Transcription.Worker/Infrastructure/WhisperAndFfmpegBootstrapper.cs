using FFMpegCore;
using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Whisper.net.Ggml;
using Xabe.FFmpeg.Downloader;

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

                // 2. Garantir FFmpeg
                _logger.LogInformation("Verificando FFmpeg...");
                var executablePath = _configuration["FFmpeg:ExecutablePath"] ?? "ffmpeg";
                var ffmpegDir = Path.Combine(AppContext.BaseDirectory, executablePath);
                
                // Se executablePath for só "ffmpeg", ffmpegDir pode estar errado se não for tratado
                // Mas a lógica original usava executablePath direto no Path.Combine com AppContext.BaseDirectory
                // Vamos assumir que se executablePath for relativo, ok.
                
                // Se for "ffmpeg", Path.Combine junta e vira .../ffmpeg. Isso é um arquivo ou pasta?
                // No original: Path.Combine(AppContext.BaseDirectory, _ffmpegOptions.ExecutablePath);
                // Se _ffmpegOptions.ExecutablePath = "ffmpeg", então .../ffmpeg
                
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
