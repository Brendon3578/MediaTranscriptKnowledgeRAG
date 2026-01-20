
using MediaTranscription.Worker.Facade;
using MediaTranscription.Worker.Infrastructure.Configuration;
using MediaTranscription.Worker.Infrastructure.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MediaTranscription.Worker;

public class TranscriptionWorker : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly ITranscriptionFacade _transcriptionFacade;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public TranscriptionWorker(
        IOptions<RabbitMqOptions> rabbitMqOptions, 
        ILogger<TranscriptionWorker> logger,
        ITranscriptionFacade transcriptionFacade)
    {
        _rabbitMqOptions = rabbitMqOptions.Value;
        _logger = logger;
        _transcriptionFacade = transcriptionFacade;
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
                    // Decide se descarta ou reenfileira; aqui ser� reenfileirado:
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    return;
                }

                await ProcessMediaAsync(mediaEvent, stoppingToken);

                // Acknowledge apenas ap�s processar com sucesso
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);

                _logger.LogInformation(
                    "Mensagem processada e confirmada. MediaId: {MediaId}",
                    mediaEvent.MediaId
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown gracioso
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");

                // Aqui voc� pode decidir:
                // - BasicNack para reprocessar
                // - BasicAck para descartar
                // - Enviar para DLQ (Dead Letter Queue)

                // Por ora, vamos fazer Nack para retentar

                if (_channel is not null)
                {
                    //await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);

                }
            }
        };
        // Registra uma �nica vez e guarda a consumer tag para cancelar no StopAsync.
        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _rabbitMqOptions.ConsumeQueue ,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        ); // consumerTag pode ser usada para BasicCancelAsync.

        // Mant�m o servi�o "vivo" at� o host pedir shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken);

    }


    private async Task ProcessMediaAsync(MediaUploadedEvent mediaEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processando media: {MediaId}, Arquivo: {FileName}, Tamanho: {Size} bytes",
            mediaEvent.MediaId,
            mediaEvent.FileName,
            mediaEvent.FileSizeBytes);

        try
        {
            var startTime = DateTime.UtcNow;

            // 1. Transcrever usando o Facade
            var text = await _transcriptionFacade.TranscribeAsync(mediaEvent.FilePath, mediaEvent.ContentType);
            
            var duration = DateTime.UtcNow - startTime;
            // Contagem simples de palavras
            var wordCount = string.IsNullOrWhiteSpace(text) 
                ? 0 
                : text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            _logger.LogInformation("Transcrição concluída para MediaId: {MediaId}. Palavras: {WordCount}", mediaEvent.MediaId, wordCount);

            /*var transcriptionData = new Transcription
            {
                
            }*/


            // 2. Publicar evento de conclusão
            var transcribedEvent = new MediaTranscribedEvent
            {
                MediaId = mediaEvent.MediaId,
                TranscriptionId = Guid.NewGuid(),
                TranscriptionText = text,
                WordCount = wordCount,
                ProcessingDuration = duration,
                TranscribedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(transcribedEvent);
            var body = Encoding.UTF8.GetBytes(json);
            
            var props = new BasicProperties
            {
                ContentType = "application/json",
                MessageId = transcribedEvent.TranscriptionId.ToString(),
                CorrelationId = mediaEvent.MediaId.ToString()
            };

            if (_channel is not null)
            {
                await _channel.BasicPublishAsync(
                    exchange: _rabbitMqOptions.ExchangeName,
                    routingKey: _rabbitMqOptions.PublishRoutingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body,
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Evento MediaTranscribed publicado para MediaId: {MediaId}", mediaEvent.MediaId);
            }
            else
            {
                _logger.LogError("Canal RabbitMQ nulo. Não foi possível publicar o evento.");
            }
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
            queue: _rabbitMqOptions.ConsumeQueue ,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _rabbitMqOptions.ConsumeQueue ,
            exchange: _rabbitMqOptions.ExchangeName,
            routingKey: _rabbitMqOptions.ConsumeRoutingKey ,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "RabbitMQ configurado: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
            _rabbitMqOptions.ExchangeName,
            _rabbitMqOptions.ConsumeQueue ,
            _rabbitMqOptions.ConsumeRoutingKey 
        );
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // StopAsync � chamado no shutdown gracioso do host; bom lugar para encerrar consumo/conex�es. [web:3]

        try
        {
            if (_channel is not null && !string.IsNullOrWhiteSpace(_consumerTag))
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken); // cancela subscription [web:23]
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao cancelar consumer.");
        }

        try
        {
            // Best practice: CloseAsync e depois DisposeAsync. [web:23]
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
            _logger.LogWarning(ex, "Falha ao fechar canal/conex�o RabbitMQ.");
        }

        await base.StopAsync(cancellationToken);
    }
}
