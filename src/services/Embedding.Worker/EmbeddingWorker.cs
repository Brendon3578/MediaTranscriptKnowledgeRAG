using MediaEmbedding.Worker.Configuration;
using MediaEmbedding.Worker.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System;
using System.Text;
using System.Text.Json;

namespace MediaEmbedding.Worker
{
    public class EmbeddingWorker : BackgroundService
    {
        private readonly RabbitMqOptions _rabbitMqOptions;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmbeddingWorker> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = false
        };


        public EmbeddingWorker(
            IOptions<RabbitMqOptions> options,
            IServiceScopeFactory scopeFactory,
            ILogger<EmbeddingWorker> logger)
        {
            _rabbitMqOptions = options.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando RabbitMqListener...");

            await InitializeRabbitMQ(stoppingToken);

            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized. Call ConnectAsync first.");


            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Mensagem recebida: {Message}", message);


                    var @event = JsonSerializer.Deserialize<MediaTranscribedEvent>(message);

                    if (@event is null)
                    {
                        _logger.LogWarning("Falha ao desserializar a mensagem.");
                        // Decide se descarta ou reenfileira; aqui ser� reenfileirado:
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                        return;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var worker = scope.ServiceProvider.GetRequiredService<MediaTranscribedConsumer>();

                    await worker.GenerateTranscriptionEmbeddingAsync(@event, stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem");
                    // Nack com requeue para tentar novamente ou enviar para DLQ
                    // Aqui faremos requeue=false para evitar loop infinito em caso de erro fatal, 
                    // mas idealmente deveria ter politica de retry/DLQ
                    if (_channel.IsOpen)
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitMqOptions.ConsumeQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task InitializeRabbitMQ(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
            };

            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Ensure Exchange and Queue exist
            await _channel.ExchangeDeclareAsync(
                exchange: _rabbitMqOptions.ExchangeName,
                type: _rabbitMqOptions.ExchangeType,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken
            );

            await _channel.QueueDeclareAsync(
                queue: _rabbitMqOptions.ConsumeQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken
            );

            await _channel.QueueBindAsync(
                queue: _rabbitMqOptions.ConsumeQueue,
                exchange: _rabbitMqOptions.ExchangeName,
                routingKey: _rabbitMqOptions.ConsumeRoutingKey,
                cancellationToken: stoppingToken
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
            if (_channel != null) await _channel.CloseAsync(cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
