using FFMpegCore;
using MediaTranscription.Worker.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaTranscription.Worker.Infrastructure.Services
{
    public interface IAudioExtractorService
    {
        Task<string> ExtractAudioAsync(string videoPath, CancellationToken cancellationToken);
    }

    public class AudioExtractorService : IAudioExtractorService
    {
        private readonly ILogger<AudioExtractorService> _logger;
        private readonly FFmpegOptions _options;

        public AudioExtractorService(ILogger<AudioExtractorService> logger, IOptions<FFmpegOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            
            // Opcional: Configurar caminho do binário se não estiver no PATH
            if (!string.IsNullOrEmpty(_options.ExecutablePath) && Path.IsPathRooted(_options.ExecutablePath))
            {
                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(_options.ExecutablePath) });
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
