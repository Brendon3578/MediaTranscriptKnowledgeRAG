using FFMpegCore;
using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Configuration;
using MediaTranscription.Worker.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text;
using System.Text.Json;

namespace MediaTranscription.Worker;

public class TranscriptionWorker : BackgroundService
{
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly ITranscriptionFacade _transcriptionFacade;
    private readonly IEventPublisher _eventPublisher;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public TranscriptionWorker(
        ILogger<TranscriptionWorker> logger,
        ITranscriptionFacade transcriptionFacade,
        IEventPublisher eventPublisher,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _logger = logger;
        _transcriptionFacade = transcriptionFacade;
        _eventPublisher = eventPublisher;
        _scopeFactory = scopeFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _logger.LogInformation("Transcription Worker iniciando...");

        await InitializeRabbitMQ(stoppingToken);

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized. Call ConnectAsync first.");


        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Mensagem recebida: {Message}", message);

                var mediaEvent = JsonSerializer.Deserialize<MediaUploadedEvent>(message);

                if (mediaEvent is null)
                {
                    _logger.LogWarning("Falha ao desserializar a mensagem.");
                    // Decide se descarta ou reenfileira; aqui será reenfileirado:
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                await ProcessMediaAsync(mediaEvent, stoppingToken);

                // Acknowledge apenas após processar com sucesso
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);

                _logger.LogInformation(
                    "Mensagem processada e confirmada. MediaId: {MediaId}",
                    mediaEvent.MediaId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");

                // Aqui é possível pode decidir:
                // - BasicNack para reprocessar
                // - BasicAck para descartar
                // - Enviar para DLQ (Dead Letter Queue)

                // Por ora, vamos fazer Nack para retentar

                if (_channel.IsOpen)
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                //await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };
        // Registra uma única vez e guarda a consumer tag para cancelar no StopAsync.
        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _rabbitMqOptions.ConsumeQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        ); // consumerTag pode ser usada para BasicCancelAsync.

        // Mantém o serviço "vivo" até o host pedir shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken);

    }


    private async Task ProcessMediaAsync(MediaUploadedEvent mediaEvent, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processando media: {MediaId}, Arquivo: {FileName}, Tamanho: {Size} bytes",
            mediaEvent.MediaId,
            mediaEvent.FileName,
            mediaEvent.FileSizeBytes);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var transcriptionDataService = scope.ServiceProvider.GetRequiredService<TranscriptionRepository>();

        try
        {
            var startTime = DateTime.UtcNow;

            // 1. Transcrever usando o Facade (Passando o modelo do evento)
            var result = await _transcriptionFacade.TranscribeAsync(mediaEvent.FilePath, mediaEvent.ContentType, mediaEvent.TranscriptionModel, ct);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Transcrição concluída para MediaId: {MediaId}. Segmentos: {Count}", mediaEvent.MediaId, result.Segments.Count);


            // Idempotência: remove segmentos existentes dessa mídia
            await transcriptionDataService.RemoveExistingTranscriptionSegmentsByMediaId(mediaEvent.MediaId, ct);

            var transcriptionId = await transcriptionDataService.SaveTranscriptionAndSegments(result, mediaEvent, ct);

            // 2. Publicar evento de conclusão
            var transcribedEvent = new MediaTranscribedEvent
            {
                MediaId = mediaEvent.MediaId,
                TranscriptionId = transcriptionId,
                TotalSegments = result.Segments.Count,
                Language = result.Language,
                ProcessingDuration = duration,
                TranscribedAt = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync(transcribedEvent, _rabbitMqOptions.PublishRoutingKey, ct);
            _logger.LogInformation("Evento MediaTranscribed publicado para MediaId: {MediaId}", transcribedEvent.MediaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no fluxo de transcrição da MediaId: {MediaId}", mediaEvent.MediaId);
            throw; // Repassa erro para lógica de retry (Nack)
        }
    }

    private async Task InitializeRabbitMQ(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.HostName,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.UserName,
            Password = _rabbitMqOptions.Password,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _rabbitMqOptions.ExchangeName,
            type: _rabbitMqOptions.ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqOptions.ConsumeQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _rabbitMqOptions.ConsumeQueue,
            exchange: _rabbitMqOptions.ExchangeName,
            routingKey: _rabbitMqOptions.ConsumeRoutingKey,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "RabbitMQ configurado: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
            _rabbitMqOptions.ExchangeName,
            _rabbitMqOptions.ConsumeQueue,
            _rabbitMqOptions.ConsumeRoutingKey
        );
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // StopAsync � chamado no shutdown gracioso do host; bom lugar para encerrar consumo/conex�es

        try
        {
            if (_channel is not null && !string.IsNullOrWhiteSpace(_consumerTag))
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken); // cancela subscription
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao cancelar consumer.");
        }

        try
        {
            // Best practice: CloseAsync e depois DisposeAsync
            if (_channel is not null)
            {
                await _channel.CloseAsync(cancellationToken: cancellationToken);
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync(cancellationToken: cancellationToken);
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao fechar canal/conexão RabbitMQ.");
        }

        await base.StopAsync(cancellationToken);
    }
}
