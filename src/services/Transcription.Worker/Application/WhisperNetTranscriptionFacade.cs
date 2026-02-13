using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Infrastructure;
using MediaTranscription.Worker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace MediaTranscription.Worker.Application
{
    public class WhisperNetTranscriptionFacade : ITranscriptionFacade
    {
        private readonly ILogger<WhisperNetTranscriptionFacade> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAudioExtractorService _audioExtractor;
        private readonly IWhisperModelProvider _modelProvider;

        public WhisperNetTranscriptionFacade(
            ILogger<WhisperNetTranscriptionFacade> logger,
            IConfiguration configuration,
            IAudioExtractorService audioExtractor,
            IWhisperModelProvider modelProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _audioExtractor = audioExtractor;
            _modelProvider = modelProvider;
        }

        public async Task<TranscriptionResultDto> TranscribeAsync(
            string filePath, 
            string contentType, 
            string? modelName, 
            Func<TranscriptionSegmentDto, Task>? onSegmentProcessed,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando fluxo de transcrição para {FilePath} com modelo {ModelName}", filePath, modelName);

            // Determina modelo a usar (Default: Medium)
            if (!Enum.TryParse<GgmlType>(modelName, true, out var modelType))
            {
                _logger.LogWarning("Modelo '{ModelName}' não reconhecido ou nulo. Usando padrão: Medium.", modelName);
                modelType = GgmlType.Medium;
            }
            
            // Garante modelo (baixa se necessário)
            var modelPath = await _modelProvider.EnsureModelAsync(modelType, cancellationToken);

            // 1. Extração/Conversão de áudio para formato compatível (WAV 16kHz mono)
            var audioPath = await _audioExtractor.ExtractAudioAsync(filePath, cancellationToken);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Carregando modelo Whisper de {ModelPath}", modelPath);

                using var whisperFactory = WhisperFactory.FromPath(modelPath);

                // Configura o builder
                var language = _configuration["Whisper:Language"] ?? "auto";
                var builder = whisperFactory.CreateBuilder()
                    .WithLanguage(language);

                using var processor = builder.Build();

                _logger.LogInformation("Processando áudio...");

                using var fileStream = File.OpenRead(audioPath);
                var currentBuffer = new List<TranscriptionSegmentDto>();
                var segmentCount = 0;
                var fullTextBuilder = new StringBuilder();
                var detectedLanguage = language;

                await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
                {
                    var rawSegment = new TranscriptionSegmentDto(
                        SegmentIndex: -1, // Temporário, será definido no Flush
                        Text: segment.Text,
                        StartSeconds: segment.Start.TotalSeconds,
                        EndSeconds: segment.End.TotalSeconds
                    );

                    currentBuffer.Add(rawSegment);

                    // Tenta capturar o idioma se vier do segmento
                    if (!string.IsNullOrWhiteSpace(segment.Language))
                    {
                         detectedLanguage = segment.Language;
                    }

                    // Lógica de Decisão para Fechar o Bloco (Flush)
                    if (ShouldFlushBuffer(currentBuffer))
                    {
                        var optimized = FlushBufferToSegment(currentBuffer, segmentCount++);
                        
                        if (fullTextBuilder.Length > 0) fullTextBuilder.Append(' ');
                        fullTextBuilder.Append(optimized.Text);

                        if (onSegmentProcessed != null)
                        {
                            await onSegmentProcessed(optimized);
                        }
                    }
                }

                // Processa o que sobrou no buffer
                if (currentBuffer.Count > 0)
                {
                    var optimized = FlushBufferToSegment(currentBuffer, segmentCount++);
                    
                    if (fullTextBuilder.Length > 0) fullTextBuilder.Append(' ');
                    fullTextBuilder.Append(optimized.Text);

                    if (onSegmentProcessed != null)
                    {
                        await onSegmentProcessed(optimized);
                    }
                }
                
                stopwatch.Stop();
                var processingTime = stopwatch.Elapsed.TotalSeconds;

                _logger.LogInformation("Transcrição concluída em {ProcessingTime}s. Modelo: {Model}. Segmentos: {Count}", 
                    processingTime, modelType, segmentCount);

                return new TranscriptionResultDto(
                    fullTextBuilder.ToString(), 
                    segmentCount, 
                    detectedLanguage, 
                    modelType.ToString(), 
                    processingTime
                );
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
                    catch { }
                }
            }
        }

        private bool ShouldFlushBuffer(List<TranscriptionSegmentDto> currentBuffer)
        {
            if (currentBuffer.Count == 0) return false;

            // Constantes de heurística
            const double MIN_DURATION_SECONDS = 30.0;
            const double MAX_DURATION_SECONDS = 90.0;
            const int MAX_TOKENS_APPROX = 600; // ~2400 caracteres

            var lastSegment = currentBuffer[^1];
            var start = currentBuffer[0].StartSeconds;
            var end = lastSegment.EndSeconds;
            var duration = end - start;
            
            var currentTextLength = currentBuffer.Sum(s => s.Text.Length);
            
            var lastText = lastSegment.Text.Trim();
            bool endsWithPunctuation = lastText.EndsWith(".") || lastText.EndsWith("?") || lastText.EndsWith("!");

            // 1. Limite máximo de tempo (Hard limit)
            if (duration >= MAX_DURATION_SECONDS) return true;

            // 2. Janela ideal (30s - 90s) + Fim de frase
            if (duration >= MIN_DURATION_SECONDS)
            {
                if (endsWithPunctuation) return true;
                
                // Se não termina com pontuação, mas já está ficando muito grande (tokens/chars), força o corte
                if (currentTextLength > (MAX_TOKENS_APPROX * 4)) return true;
            }

            return false;
        }

        private TranscriptionSegmentDto FlushBufferToSegment(List<TranscriptionSegmentDto> buffer, int index)
        {
            var first = buffer[0];
            var last = buffer[^1];

            var sb = new StringBuilder();
            foreach (var seg in buffer)
            {
                var text = seg.Text.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                
                if (sb.Length > 0 && !char.IsWhiteSpace(sb[^1]))
                {
                    sb.Append(' ');
                }
                sb.Append(text);
            }

            var optimized = new TranscriptionSegmentDto(
                SegmentIndex: index,
                Text: sb.ToString(),
                StartSeconds: first.StartSeconds,
                EndSeconds: last.EndSeconds
            );

            buffer.Clear();
            return optimized;
        }
    }
}
