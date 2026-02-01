using System.ComponentModel.DataAnnotations;

namespace MediaEmbedding.Worker.Configuration
{
    public class RabbitMqOptions
    {
        [Required]
        public string HostName { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ExchangeName { get; set; } = string.Empty;

        [Required]
        public string ExchangeType { get; set; } = string.Empty;

        public string PublishRoutingKey { get; set; } = "media.embedded";
    }
}
