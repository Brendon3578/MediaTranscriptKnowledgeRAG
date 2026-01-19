namespace Upload.Api.Infrastructure.Configuration
{
    public class RabbitMqConfiguration
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        public string ExchangeType { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;


        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(HostName))
                throw new ArgumentException("RabbitMQ HostName is not configured.");
            if (Port <= 0)
                throw new ArgumentException("RabbitMQ Port must be a positive integer.");
            if (string.IsNullOrWhiteSpace(UserName))
                throw new ArgumentException("RabbitMQ UserName is not configured.");
            if (string.IsNullOrWhiteSpace(Password))
                throw new ArgumentException("RabbitMQ Password is not configured.");
            if (string.IsNullOrWhiteSpace(ExchangeName))
                throw new ArgumentException("RabbitMQ ExchangeName is not configured.");
            if (string.IsNullOrWhiteSpace(ExchangeType))
                throw new ArgumentException("RabbitMQ ExchangeType is not configured.");
            if (string.IsNullOrWhiteSpace(RoutingKey))
                throw new ArgumentException("RabbitMQ RoutingKey is not configured.");
            if (string.IsNullOrWhiteSpace(QueueName))
                throw new ArgumentException("RabbitMQ QueueName is not configured.");
        }
    }
}
