using System.Text.Json;

namespace Upload.Api.Messaging
{
    public class InMemoryMessagePublisher : IMessagePublisher
    {
        private readonly ILogger<InMemoryMessagePublisher> _logger;

        public InMemoryMessagePublisher(ILogger<InMemoryMessagePublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation(
                "Evento publicado [{EventType}]: {EventData}",
                typeof(T).Name,
                json
            );

            // TODO: Integrar com RabbitMQ
            return Task.CompletedTask;
        }
    }
}
