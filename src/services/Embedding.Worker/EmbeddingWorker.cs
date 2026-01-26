using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmbeddingWorker> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public EmbeddingWorker(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<EmbeddingWorker> logger)
        {
            _configuration = configuration;
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
                        // Decide se descarta ou reenfileira; aqui será reenfileirado:
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
                queue: _configuration["RabbitMq:ConsumeQueue"] ?? "embedding.media.transcribed",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task InitializeRabbitMQ(CancellationToken ct)
        {
            var hostname = _configuration["RabbitMq:HostName"] ?? throw new InvalidOperationException("RabbitMq:HostName is not configured.");
            var port = _configuration.GetValue<int?>("RabbitMq:Port") ?? 5672;
            var username = _configuration["RabbitMq:UserName"] ?? throw new InvalidOperationException
                ("RabbitMq:UserName is not configured.");
            var password = _configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("RabbitMq:Password is not configured.");
            var consumeQueue = _configuration["RabbitMq:ConsumeQueue"] ?? throw new InvalidOperationException
                ("RabbitMq:ConsumeQueue is not configured.");

            var exchangeName = _configuration["RabbitMq:ExchangeName"] ?? throw new InvalidOperationException
                ("RabbitMq:ExchangeName is not configured.");

            var exchangeType = _configuration["RabbitMq:ExchangeType"] ?? throw new InvalidOperationException
                ("RabbitMq:ExchangeType is not configured.");

            var consumeRoutingKey = _configuration["RabbitMq:ConsumeRoutingKey"] ?? throw new InvalidOperationException
                ("RabbitMq:ConsumeRoutingKey is not configured.");

            var factory = new ConnectionFactory
            {
                HostName = hostname,
                Port = port,
                UserName = username,
                Password = password,
            };

            // Retry logic
            var retryCount = 3;
            while (retryCount > 0)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(ct);
                    _channel = await _connection.CreateChannelAsync(null, ct);

                    await _channel.ExchangeDeclareAsync(
                        exchange: exchangeName,
                        type: exchangeType,
                        durable: true,
                        autoDelete: false,
                        cancellationToken: ct
                    );

                    await _channel.QueueDeclareAsync(
                        queue: consumeQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: ct
                    );

                    await _channel.QueueBindAsync(
                        queue: consumeQueue,
                        exchange: exchangeName,
                        routingKey: consumeRoutingKey,
                        cancellationToken: ct
                    );

                    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

                    _logger.LogInformation("Conectado ao RabbitMQ.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao conectar no RabbitMQ. Tentando novamente em 5s...");
                    retryCount--;
                    await Task.Delay(5000, ct);
                }
            }

            throw new Exception("Não foi possível conectar ao RabbitMQ após várias tentativas.");
        }
    }
}
