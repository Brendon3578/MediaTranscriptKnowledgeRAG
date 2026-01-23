using MediaTranscription.Worker.Configuration;
using MediaTranscription.Worker.Infrastructure.Services;
using MediaTranscription.Worker.Interfaces;
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

        public async Task<TranscriptionResultDto> TranscribeAsync(string filePath, string contentType, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando fluxo de transcrição para {FilePath}", filePath);

            // Garante que o modelo existe (baixa se necessário) na primeira execução ou recupera do cache
            if (string.IsNullOrEmpty(_cachedModelPath))
            {
                _cachedModelPath = await _modelProvider.EnsureModelAsync(cancellationToken);
            }

            // 1. Extração/Conversão de áudio para formato compatível (WAV 16kHz mono)
            var audioPath = await _audioExtractor.ExtractAudioAsync(filePath, cancellationToken);

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
                var segments = new List<TranscriptionSegmentDto>();
                var index = 0;
                var language = _options.Language ?? "auto";

                // final text transcript
                var sb = new StringBuilder();

                
                await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
                {
                    segments.Add(new TranscriptionSegmentDto(
                        SegmentIndex: index++,
                        Text: segment.Text,
                        StartSeconds: segment.Start.TotalSeconds, // validar se realmente é um valor double
                        EndSeconds: segment.End.TotalSeconds
                    ));

                    sb.Append(segment.Text);

                    language = string.IsNullOrWhiteSpace(segment.Language) ? language : segment.Language;
                }

                var transcription = sb.ToString().Trim();

                _logger.LogInformation("Transcrição concluída. Segmentos: {Count}", segments.Count);
                return new TranscriptionResultDto(transcription, segments, language);
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
