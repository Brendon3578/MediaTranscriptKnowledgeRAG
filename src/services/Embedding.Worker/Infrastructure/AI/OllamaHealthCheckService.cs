using System.Net.Http;
using MediaEmbedding.Worker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaEmbedding.Worker.Infrastructure.AI
{
    public class OllamaHealthCheckService : IOllamaHealthCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaHealthCheckService> _logger;
        private readonly string _baseUrl;

        public OllamaHealthCheckService(
            HttpClient httpClient,
            ILogger<OllamaHealthCheckService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _baseUrl = configuration["Ollama:BaseUrl"]
                       ?? configuration["Embedding:BaseUrl"]
                       ?? "http://localhost:11434";

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task CheckAvailabilityAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.GetAsync("/", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ollama is available at {BaseUrl}.", _baseUrl);
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var message =
                    $"Ollama health check failed with status code {(int)response.StatusCode} ({response.StatusCode}) at '{_baseUrl}'. Response: {body}";

                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                _logger.LogError(ex,
                    "Failed to reach Ollama at {BaseUrl}. The embedding worker will not start.",
                    _baseUrl);
                throw;
            }
        }
    }
}

