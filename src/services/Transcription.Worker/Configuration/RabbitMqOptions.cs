using System.ComponentModel.DataAnnotations;

namespace MediaTranscription.Worker.Configuration
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
        public string ConsumeRoutingKey  { get; set; } = string.Empty;
        [Required]
        public string ConsumeQueue  { get; set; } = string.Empty;
        [Required]
        public string PublishRoutingKey { get; set; } = string.Empty;




    }
}
