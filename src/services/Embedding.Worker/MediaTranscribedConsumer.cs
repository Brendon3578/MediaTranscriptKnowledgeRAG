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
                // Ensure status is Processing (recover from Failed if retrying)
                await _statusUpdater.UpdateStatusAsync(@event.MediaId, MediaStatus.Processing, MediaStatus.Failed, ct);

                // 1. Busca segmentos
                var segments = await _embeddingDataService.FindTranscriptionSegmentsByMediaIdAsync(@event.MediaId);

                if (segments.Count == 0)
                {
                    _logger.LogWarning("Nenhum segmento encontrado para MediaId: {MediaId}", @event.MediaId);
                    return;
                }

                _logger.LogInformation("Encontrados {Count} segmentos para processar.", segments.Count);

                int processedCount = 0;
                int skippedCount = 0;

                foreach (var segment in segments)
                {
                    // 2. Idempotência: verifica se já existe embedding para este segmento e modelo
                    bool exists = await _embeddingDataService.EmbeddingExistsForSegmentAsync(segment.Id, _modelName);

                    if (exists)
                    {
                        skippedCount++;
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
                    processedCount++;
                }

                if (processedCount > 0)
                {
                    await _embeddingDataService.SaveEmbeddingsAsync();

                    await PublishMediaEmbeddedEventAsync(@event.MediaId, _modelName, segments.Count, ct);

                    _logger.LogInformation(
                        "MediaEmbedded publicado - MediaId: {MediaId}, Chunks: {ChunksCount}, Model: {ModelName}",
                        @event.MediaId, segments.Count, _modelName);
                }

                _logger.LogInformation("Processamento concluído para MediaId: {MediaId}. Gerados: {Processed}, Pulados: {Skipped}",
                    @event.MediaId, processedCount, skippedCount);

                await _statusUpdater.UpdateStatusAsync(@event.MediaId, MediaStatus.Completed, MediaStatus.Processing, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar embeddings para MediaId: {MediaId}", @event.MediaId);
                try
                {
                    await _statusUpdater.UpdateStatusAsync(@event.MediaId, MediaStatus.Failed, MediaStatus.Processing, ct);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update status to Failed for MediaId: {MediaId}", @event.MediaId);
                }
                throw; // Re-throw para o RabbitMQ fazer Nack/Retry
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
