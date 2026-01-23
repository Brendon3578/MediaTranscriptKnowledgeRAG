using System.ComponentModel.DataAnnotations;

namespace Query.Api.Configuration
{
    public class ChatConfigOptions
    {
        [Required]
        public string Model { get; set; } = string.Empty;
        [Required]
        public string BaseUrl { get; set; } = string.Empty;
    }
}
