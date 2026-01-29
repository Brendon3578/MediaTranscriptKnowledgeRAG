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

        public async Task<TranscriptionResultDto> TranscribeAsync(string filePath, string contentType, string? modelName, CancellationToken cancellationToken)
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
                var segments = new List<TranscriptionSegmentDto>();
                var index = 0;
                var detectedLanguage = language;

                await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
                {
                    segments.Add(new TranscriptionSegmentDto(
                        SegmentIndex: index++,
                        Text: segment.Text,
                        StartSeconds: segment.Start.TotalSeconds,
                        EndSeconds: segment.End.TotalSeconds
                    ));

                    // Tenta capturar o idioma se vier do segmento
                    if (!string.IsNullOrWhiteSpace(segment.Language))
                    {
                         detectedLanguage = segment.Language;
                    }
                }
                
                stopwatch.Stop();
                var processingTime = stopwatch.Elapsed.TotalSeconds;

                // --- PÓS-PROCESSAMENTO PARA RAG ---
                var optimizedSegments = EnhanceTranscriptionSegments(segments);
                
                // Recalcula o texto completo baseado nos segmentos otimizados
                var finalTranscription = string.Join(" ", optimizedSegments.Select(s => s.Text));

                _logger.LogInformation("Transcrição concluída em {ProcessingTime}s. Modelo: {Model}. Segmentos: {Count}", 
                    processingTime, modelType, optimizedSegments.Count);

                return new TranscriptionResultDto(
                    finalTranscription, 
                    optimizedSegments, 
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

        private List<TranscriptionSegmentDto> EnhanceTranscriptionSegments(List<TranscriptionSegmentDto> rawSegments)
        {
            if (rawSegments == null || rawSegments.Count == 0)
                return new List<TranscriptionSegmentDto>();

            var optimized = new List<TranscriptionSegmentDto>();
            var currentBuffer = new List<TranscriptionSegmentDto>();
            
            // Constantes de heurística
            const double MIN_DURATION_SECONDS = 30.0;
            const double MAX_DURATION_SECONDS = 90.0;
            const int MIN_TOKENS_APPROX = 300; // ~1200 caracteres
            const int MAX_TOKENS_APPROX = 600; // ~2400 caracteres

            foreach (var segment in rawSegments)
            {
                currentBuffer.Add(segment);

                // Calcula métricas do buffer atual
                var start = currentBuffer[0].StartSeconds;
                var end = currentBuffer[currentBuffer.Count - 1].EndSeconds;
                var duration = end - start;
                
                // Texto acumulado (estimativa de tamanho)
                var currentTextLength = currentBuffer.Sum(s => s.Text.Length);
                
                // Heurística de pontuação (fim de frase)
                var lastText = segment.Text.Trim();
                bool endsWithPunctuation = lastText.EndsWith(".") || lastText.EndsWith("?") || lastText.EndsWith("!");

                // Lógica de Decisão para Fechar o Bloco (Flush)
                
                // 1. Limite máximo de tempo (Hard limit)
                if (duration >= MAX_DURATION_SECONDS)
                {
                    FlushBuffer(optimized, currentBuffer);
                    continue;
                }

                // 2. Janela ideal (30s - 90s) + Fim de frase
                if (duration >= MIN_DURATION_SECONDS)
                {
                    if (endsWithPunctuation)
                    {
                        FlushBuffer(optimized, currentBuffer);
                        continue;
                    }
                    
                    // Se não termina com pontuação, mas já está ficando muito grande (tokens/chars), força o corte
                    if (currentTextLength > (MAX_TOKENS_APPROX * 4)) 
                    {
                         FlushBuffer(optimized, currentBuffer);
                         continue;
                    }
                }
            }

            // Processa o que sobrou no buffer
            if (currentBuffer.Count > 0)
            {
                FlushBuffer(optimized, currentBuffer);
            }

            return optimized;
        }

        private void FlushBuffer(List<TranscriptionSegmentDto> optimized, List<TranscriptionSegmentDto> buffer)
        {
            if (buffer.Count == 0) return;

            var first = buffer[0];
            var last = buffer[buffer.Count - 1];

            // Concatena textos com espaço adequado
            var sb = new StringBuilder();
            foreach (var seg in buffer)
            {
                if (sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]))
                    sb.Append(" ");
                
                sb.Append(seg.Text.Trim());
            }

            var mergedSegment = new TranscriptionSegmentDto(
                SegmentIndex: optimized.Count, // Novo índice sequencial
                Text: sb.ToString(),
                StartSeconds: first.StartSeconds,
                EndSeconds: last.EndSeconds
            );

            optimized.Add(mergedSegment);
            buffer.Clear();
        }
    }
}
