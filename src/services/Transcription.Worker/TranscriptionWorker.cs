
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading;
using Upload.Api.Infrastructure.Configuration;

namespace MediaTranscription.Worker;

public class TranscriptionWorker : BackgroundService
{
    private readonly RabbitMqConfiguration _rabbitMqConfig;
    private readonly ILogger<TranscriptionWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;

    public TranscriptionWorker(RabbitMqConfiguration rabbitMqConfig, ILogger<TranscriptionWorker> logger)
    {
        _rabbitMqConfig = rabbitMqConfig;
        _logger = logger;
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown gracioso
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");

                // Aqui você pode decidir:
                // - BasicNack para reprocessar
                // - BasicAck para descartar
                // - Enviar para DLQ (Dead Letter Queue)

                // Por ora, vamos fazer Nack para retentar

                if (_channel is not null)
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };
        // Registra uma única vez e guarda a consumer tag para cancelar no StopAsync.
        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _rabbitMqConfig.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        ); // consumerTag pode ser usada para BasicCancelAsync.

        // Mantém o serviço "vivo" até o host pedir shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken);

    }


    private async Task ProcessMediaAsync(MediaUploadedEvent mediaEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processando media: {MediaId}, Arquivo: {FileName}, Tamanho: {Size} bytes",
            mediaEvent.MediaId,
            mediaEvent.FileName,
            mediaEvent.FileSizeBytes);

        // Aqui será implementado a lógica de transcrição
        // Por enquanto, apenas simulamos processamento
        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("Transcrição concluída para MediaId: {MediaId}", mediaEvent.MediaId);

        // TODO: Publicar evento TranscriptionCompleted
    }

    private async Task InitializeRabbitMQ(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqConfig.HostName,
            Port = _rabbitMqConfig.Port,
            UserName = _rabbitMqConfig.UserName,
            Password = _rabbitMqConfig.Password,
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _rabbitMqConfig.ExchangeName,
            type: _rabbitMqConfig.ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqConfig.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await _channel.QueueBindAsync(
            queue: _rabbitMqConfig.QueueName,
            exchange: _rabbitMqConfig.ExchangeName,
            routingKey: _rabbitMqConfig.RoutingKey,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "RabbitMQ configurado: Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
            _rabbitMqConfig.ExchangeName,
            _rabbitMqConfig.QueueName,
            _rabbitMqConfig.RoutingKey
        );
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // StopAsync é chamado no shutdown gracioso do host; bom lugar para encerrar consumo/conexões. [web:3]

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
            _logger.LogWarning(ex, "Falha ao fechar canal/conexão RabbitMQ.");
        }

        await base.StopAsync(cancellationToken);
    }
}
