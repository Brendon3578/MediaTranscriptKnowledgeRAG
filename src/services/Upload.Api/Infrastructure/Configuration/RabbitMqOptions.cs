using System.ComponentModel.DataAnnotations;

namespace Upload.Api.Infrastructure.Configuration
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
        [Required]
        public string RoutingKey { get; set; } = string.Empty;
    }
}
