using System.Net.Http;
using Microsoft.Extensions.Logging;
using Query.Api.Application;

namespace Query.Api.Infrastructure
{
    public class OllamaHealthCheckService : IOllamaHealthCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaHealthCheckService> _logger;
        private readonly string _embeddingBaseUrl;
        private readonly string _chatBaseUrl;

        public OllamaHealthCheckService(
            HttpClient httpClient,
            ILogger<OllamaHealthCheckService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _embeddingBaseUrl = configuration["Ollama:BaseUrl"]
                                ?? configuration["Embedding:BaseUrl"]
                                ?? "http://localhost:11434";

            _chatBaseUrl = configuration["Chat:BaseUrl"]
                           ?? _embeddingBaseUrl;
        }

        public async Task CheckAvailabilityAsync(CancellationToken cancellationToken)
        {
            // Check embedding endpoint
            await CheckEndpointAsync(_embeddingBaseUrl, "Embedding", cancellationToken);

            // Check chat endpoint (can be same base URL, but we validate explicitly)
            if (!string.Equals(_chatBaseUrl, _embeddingBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                await CheckEndpointAsync(_chatBaseUrl, "Chat", cancellationToken);
            }
        }

        private async Task CheckEndpointAsync(string baseUrl, string purpose, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), "/"));
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ollama {Purpose} endpoint is available at {BaseUrl}.", purpose, baseUrl);
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var message =
                    $"Ollama {purpose} health check failed with status code {(int)response.StatusCode} ({response.StatusCode}) at '{baseUrl}'. Response: {body}";

                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                _logger.LogError(ex,
                    "Failed to reach Ollama {Purpose} endpoint at {BaseUrl}. Query.Api will not start.",
                    purpose,
                    baseUrl);
                throw;
            }
        }
    }
}

