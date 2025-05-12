using InventoryService.Models;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace InventoryService.Services
{
    public class WebhookService
    {
        private readonly ConcurrentDictionary<string, WebhookRegistration> _webhooks = new();
        private readonly ILogger<WebhookService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookService(ILogger<WebhookService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public WebhookRegistration RegisterWebhook(string callbackUrl)
        {
            var registration = new WebhookRegistration
            {
                CallbackUrl = callbackUrl
            };

            _webhooks.TryAdd(registration.Id, registration);
            _logger.LogInformation($"Registered webhook with ID {registration.Id} for URL {callbackUrl}");
            
            return registration;
        }

        public async Task NotifyAllAsync(InventoryJob job)
        {
            var client = _httpClientFactory.CreateClient();
            var tasks = new List<Task>();

            foreach (var webhook in _webhooks.Values)
            {
                tasks.Add(NotifyWebhookAsync(client, webhook, job));
            }

            await Task.WhenAll(tasks);
        }

        private async Task NotifyWebhookAsync(HttpClient client, WebhookRegistration webhook, InventoryJob job)
        {
            try
            {
                var response = await client.PostAsJsonAsync(webhook.CallbackUrl, job);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation($"Successfully notified webhook {webhook.Id} about job {job.JobId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to notify webhook {webhook.Id} about job {job.JobId}");
            }
        }
    }
}
