using MediaEmbedding.Worker.Application.Interfaces;
using MediaEmbedding.Worker.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MediaEmbedding.Worker.Infrastructure.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private IChannel? _channel;
        private IConnection? _connection;
        private readonly RabbitMqOptions _rabbitMqOptions;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = false };

        public RabbitMqEventPublisher(IOptions<RabbitMqOptions> rabbitMqOptions, ILogger<RabbitMqEventPublisher> logger)
        {
            _rabbitMqOptions = rabbitMqOptions.Value;
            _logger = logger;
        }

        public async Task ConnectAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(null, ct);

            await _channel.ExchangeDeclareAsync(
                exchange: _rabbitMqOptions.ExchangeName,
                type: _rabbitMqOptions.ExchangeType,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            _logger.LogInformation("RabbitMQ publisher conectado - Exchange: {Exchange}", _rabbitMqOptions.ExchangeName);
        }

        public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : class
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized. Call ConnectAsync first.");

            var json = JsonSerializer.Serialize(@event, _jsonSerializerOptions);
            var rawBody = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type = typeof(T).Name
            };

            await _channel.BasicPublishAsync(
                exchange: _rabbitMqOptions.ExchangeName,
                routingKey: routingKey,
                body: rawBody,
                cancellationToken: ct,
                mandatory: false,
                basicProperties: properties);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
                await _channel.CloseAsync();
            if (_connection != null)
                await _connection.CloseAsync();
        }
    }
}
