using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace MediaTranscription.Worker.Infrastructure
{
    public interface IAudioExtractorService
    {
        Task<string> ExtractAudioAsync(string videoPath, CancellationToken cancellationToken);
    }

    public class AudioExtractorService : IAudioExtractorService
    {
        private readonly ILogger<AudioExtractorService> _logger;
        private readonly IConfiguration _configuration;

        public AudioExtractorService(ILogger<AudioExtractorService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Opcional: Configurar caminho do binário se não estiver no PATH
            var executablePath = _configuration["FFmpeg:ExecutablePath"] ?? "ffmpeg";
            
            if (!string.IsNullOrEmpty(executablePath) && Path.IsPathRooted(executablePath))
            {
                var ffmpegDir = Path.GetDirectoryName(executablePath);
                if (string.IsNullOrEmpty(ffmpegDir))
                    throw new InvalidOperationException("FFMpeg directory not defined.");

                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });
            }
        }

        public async Task<string> ExtractAudioAsync(string videoPath, CancellationToken cancellationToken)
        {
            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Arquivo de vídeo não encontrado: {videoPath}");

            // Gera caminho para o arquivo WAV temporário
            var outputFile = Path.ChangeExtension(videoPath, ".wav");
            
            // Se o arquivo de origem já for wav, podemos querer apenas garantir o formato, 
            // mas por simplificação, vamos processar sempre para garantir 16kHz mono.
            
            _logger.LogInformation("Extraindo áudio de {VideoPath} para {AudioPath}", videoPath, outputFile);


            try 
            {
                // Converte para WAV 16kHz Mono (PCM 16-bit)
                await FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(outputFile, true, options => options
                        .WithAudioSamplingRate(16000)
                        .WithAudioCodec("pcm_s16le")
                        .ForceFormat("wav")
                        .WithCustomArgument("-ac 1")) // Mono
                    .ProcessAsynchronously();

                return outputFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao extrair áudio de {VideoPath}", videoPath);
                throw;
            }
        }
    }
}
