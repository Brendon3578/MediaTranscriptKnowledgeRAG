namespace Upload.Api.Messaging
{
    public class RabbitMqHostedService : IHostedService
    {
        private readonly RabbitMqEventPublisher _publisher;
        private readonly ILogger<RabbitMqHostedService> _logger;

        public RabbitMqHostedService(
            RabbitMqEventPublisher publisher,
            ILogger<RabbitMqHostedService> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting RabbitMQ connection...");
            await _publisher.ConnectAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping RabbitMQ connection...");
            await _publisher.DisposeAsync();
        }
    }
}
