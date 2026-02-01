namespace MediaEmbedding.Worker.Infrastructure.Messaging
{
    public class RabbitMqPublisherHostedService : IHostedService
    {
        private readonly RabbitMqEventPublisher _publisher;
        private readonly ILogger<RabbitMqPublisherHostedService> _logger;

        public RabbitMqPublisherHostedService(
            RabbitMqEventPublisher publisher,
            ILogger<RabbitMqPublisherHostedService> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Conectando RabbitMQ publisher...");
            await _publisher.ConnectAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Encerrando RabbitMQ publisher...");
            await _publisher.DisposeAsync();
        }
    }
}
