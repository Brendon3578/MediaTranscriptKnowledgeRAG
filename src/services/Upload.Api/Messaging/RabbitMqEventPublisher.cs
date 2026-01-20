
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Upload.Api.Infrastructure.Configuration;

namespace Upload.Api.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private IChannel? _channel; // antes era IModel
        private IConnection? _connection;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = false
        };


        public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        // chamar no program.cs ou startup.cs
        public async Task ConnectAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(null, ct);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType, // topic
                durable: true,
                autoDelete: false,
                cancellationToken: ct
            );

            _logger.LogInformation(
                "RabbitMQ conectado - Exchange: {Exchange}, Host: {Host}",
                _options.ExchangeName,
                _options.HostName
            );
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
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    body: rawBody,
                    cancellationToken: ct,
                    mandatory: false, // TODO: validar isso depois
                    basicProperties: properties
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao publicar evento - Type: {EventType}, RoutingKey: {RoutingKey}",
                    typeof(T).Name, routingKey
                );
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                await _channel.CloseAsync(); ;
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _logger.LogInformation("RabbitMQ connection closed");
        }
    }
}
