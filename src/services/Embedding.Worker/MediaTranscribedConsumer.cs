using MediaEmbedding.Worker.Application.Interfaces;
using MediaEmbedding.Worker.Application.UseCases;
using MediaEmbedding.Worker.Configuration;
using MediaEmbedding.Worker.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using Shared.Contracts.Events;

namespace MediaEmbedding.Worker
{
    public class MediaTranscribedConsumer
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<MediaTranscribedConsumer> _logger;
        private readonly GenerateEmbeddingUseCase _embeddingDataService;
        private readonly IMediaStatusUpdater _statusUpdater;
        private readonly string _modelName;
        private readonly string _publishRoutingKey;

        public MediaTranscribedConsumer(
            GenerateEmbeddingUseCase embeddingDataService,
            IEmbeddingService embeddingService,
            IEventPublisher eventPublisher,
            IMediaStatusUpdater statusUpdater,
            IOptions<RabbitMqOptions> rabbitMqOptions,
            IConfiguration configuration,
            ILogger<MediaTranscribedConsumer> logger)
        {
            _embeddingDataService = embeddingDataService;
            _embeddingService = embeddingService;
            _eventPublisher = eventPublisher;
            _statusUpdater = statusUpdater;
            _logger = logger;
            _modelName = configuration["Embedding:Model"] ?? "nomic-embed-text";
            _publishRoutingKey = rabbitMqOptions.Value.PublishRoutingKey;
        }

        public async Task GenerateTranscriptionEmbeddingAsync(MediaTranscribedEvent @event, CancellationToken ct)
        {
            _logger.LogInformation("Iniciando processamento de embeddings para MediaId: {MediaId}", @event.MediaId);

            try
            {
                // Ensure status is EmbeddingProcessing (from TranscriptionCompleted or recover from Failed if retrying)
                var updated = await _statusUpdater.UpdateStatusAsync(@event.MediaId, MediaStatus.EmbeddingProcessing, MediaStatus.TranscriptionCompleted, ct);
                if (!updated)
                { 
                    updated = await _statusUpdater.UpdateStatusAsync(@event.MediaId, MediaStatus.EmbeddingProcessing, MediaStatus.Failed, ct);
                    if (!updated)
                    {
                        _logger.LogWarning("Media {MediaId} could not be transitioned to EmbeddingProcessing. It might be in an unexpected state. Skipping.", @event.MediaId);
                        return;
                    }
                }

                // Initialize progress
                await _statusUpdater.UpdateProgressAsync(@event.MediaId, 0, ct);

                // 1. Busca segmentos
                var segments = await _embeddingDataService.FindTranscriptionSegmentsByMediaIdAsync(@event.MediaId);

                if (segments.Count == 0)
                {
                    _logger.LogWarning("Nenhum segmento encontrado para MediaId: {MediaId}", @event.MediaId);
                    await _statusUpdater.UpdateFinalStateAsync(@event.MediaId, MediaStatus.Completed, ct);
                    return;
                }

                _logger.LogInformation("Encontrados {Count} segmentos para processar.", segments.Count);

                int totalSegments = segments.Count;
                int processedSegments = 0;
                int skippedCount = 0;

                foreach (var segment in segments)
                {
                    // 2. Idempotência: verifica se já existe embedding para este segmento e modelo
                    bool exists = await _embeddingDataService.EmbeddingExistsForSegmentAsync(segment.Id, _modelName);

                    if (exists)
                    {
                        skippedCount++;
                        processedSegments++; // Consider skipped as processed for progress calculation
                        continue;
                    }

                    // 3. Gera embedding
                    var vector = await _embeddingService.GenerateAsync(segment.Text, ct);

                    // 4. Salva no banco
                    var embedding = new EmbeddingEntity
                    { 
                        Id = Guid.NewGuid(),
                        MediaId = segment.MediaId,
                        TranscriptionId = segment.TranscriptionId,
                        TranscriptionSegmentId = segment.Id,
                        ModelName = _modelName,
                        ChunkText = segment.Text,
                        EmbeddingVector = new Vector(vector),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _embeddingDataService.SaveEmbeddingAsync(embedding);
                    await _embeddingDataService.SaveEmbeddingsAsync(); // Persist each one for progress reliability

                    processedSegments++;
                    float progress = ((float)processedSegments / totalSegments) * 100;
                    await _statusUpdater.UpdateProgressAsync(@event.MediaId, progress, ct);
                }

                _logger.LogInformation("Processamento concluído para MediaId: {MediaId}. Processados: {Processed}, Pulados: {Skipped}", 
                    @event.MediaId, totalSegments - skippedCount, skippedCount);

                // Finalize state
                await _statusUpdater.UpdateFinalStateAsync(@event.MediaId, MediaStatus.Completed, ct);

                // Publish event
                await PublishMediaEmbeddedEventAsync(@event.MediaId, _modelName, totalSegments, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar embeddings para MediaId: {MediaId}", @event.MediaId);
                await _statusUpdater.UpdateFinalStateAsync(@event.MediaId, MediaStatus.Failed, ct);
            }
        }

        private async Task PublishMediaEmbeddedEventAsync(Guid mediaId, string modelName, int chunksCount, CancellationToken ct)
        {
            var embeddedEvent = new MediaEmbeddedEvent
            {
                MediaId = mediaId,
                ModelName = modelName,
                ChunksCount = chunksCount,
                EmbeddedAt = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync(embeddedEvent, _publishRoutingKey, ct);
        }
    }
}
