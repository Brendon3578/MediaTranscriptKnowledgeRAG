using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MediaTranscription.Worker.Infrastructure.Messaging
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

            _channel.BasicReturnAsync += async (_, args) =>
            {
                var eventType = args.BasicProperties?.Type ?? "unknown";
                _logger.LogError(
                    "Unroutable message returned by RabbitMQ | Exchange={Exchange} | RoutingKey={RoutingKey} | EventType={EventType}",
                    args.Exchange,
                    args.RoutingKey,
                    eventType);
            };

            _logger.LogInformation(
                "RabbitMQ conectado - Exchange: {Exchange}, Host: {Host}",
                _rabbitMqOptions.ExchangeName,
                _rabbitMqOptions.HostName);
        }

        public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : class
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized. Call ConnectAsync first.");

            try
            {
                var json = JsonSerializer.Serialize(@event, _jsonSerializerOptions);
                var rawBody = Encoding.UTF8.GetBytes(json);
                var properties = new BasicProperties()
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
                    mandatory: true,
                    basicProperties: properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event | Exchange={Exchange} | RoutingKey={RoutingKey} | EventType={EventType}",
                    _rabbitMqOptions.ExchangeName,
                    routingKey,
                    typeof(T).Name);
                throw;
            }
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
