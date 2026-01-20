using MediaTranscription.Worker.Infrastructure.Configuration;
using MediaTranscription.Worker.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Whisper.net;

namespace MediaTranscription.Worker.Facade
{
    public class WhisperNetTranscriptionFacade : ITranscriptionFacade
    {
        private readonly ILogger<WhisperNetTranscriptionFacade> _logger;
        private readonly WhisperOptions _options;
        private readonly IAudioExtractorService _audioExtractor;
        private readonly IWhisperModelProvider _modelProvider;
        private string? _cachedModelPath;

        public WhisperNetTranscriptionFacade(
            ILogger<WhisperNetTranscriptionFacade> logger,
            IOptions<WhisperOptions> options,
            IAudioExtractorService audioExtractor,
            IWhisperModelProvider modelProvider)
        {
            _logger = logger;
            _options = options.Value;
            _audioExtractor = audioExtractor;
            _modelProvider = modelProvider;
        }

        public async Task<string> TranscribeAsync(string filePath, string contentType)
        {
            _logger.LogInformation("Iniciando fluxo de transcrição para {FilePath}", filePath);

            // Garante que o modelo existe (baixa se necessário) na primeira execução ou recupera do cache
            if (string.IsNullOrEmpty(_cachedModelPath))
            {
                _cachedModelPath = await _modelProvider.EnsureModelAsync(CancellationToken.None);
            }

            // 1. Extração/Conversão de áudio para formato compatível (WAV 16kHz mono)
            var audioPath = await _audioExtractor.ExtractAudioAsync(filePath, CancellationToken.None);

            try
            {
                _logger.LogInformation("Carregando modelo Whisper de {ModelPath}", _cachedModelPath);

                using var whisperFactory = WhisperFactory.FromPath(_cachedModelPath);

                // Configura o builder
                var builder = whisperFactory.CreateBuilder()
                    .WithLanguage(_options.Language ?? "auto");

                using var processor = builder.Build();

                _logger.LogInformation("Processando áudio...");

                using var fileStream = File.OpenRead(audioPath);
                var sb = new StringBuilder();

                var languageList = new HashSet<string>();

                await foreach (var segment in processor.ProcessAsync(fileStream))
                {
                    sb.Append(segment.Text);
                    languageList.Add(segment.Language);
                }

                var transcription = sb.ToString().Trim();
                _logger.LogInformation("Transcrição concluída. Tamanho do texto: {Length}", transcription.Length);

                return transcription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a transcrição do arquivo {AudioPath}", audioPath);
                throw;
            }
            finally
            {
                // Limpeza do arquivo WAV temporário
                if (!string.Equals(audioPath, filePath, StringComparison.OrdinalIgnoreCase) && File.Exists(audioPath))
                {
                    try
                    {
                        File.Delete(audioPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Não foi possível remover o arquivo temporário {AudioPath}", audioPath);
                    }
                }
            }
        }
    }
}
