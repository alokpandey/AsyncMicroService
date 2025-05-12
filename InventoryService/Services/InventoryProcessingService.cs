using InventoryService.Models;
using System.Text.Json;

namespace InventoryService.Services
{
    /// <summary>
    /// This service simulates a Kafka consumer by consuming messages from the KafkaTopicService
    /// In a real-world scenario, this would use the Confluent.Kafka library to consume messages from a Kafka topic
    /// </summary>
    public class InventoryProcessingService : BackgroundService
    {
        private readonly ILogger<InventoryProcessingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly WebhookService _webhookService;
        private readonly KafkaTopicService _kafkaTopicService;

        public InventoryProcessingService(
            ILogger<InventoryProcessingService> logger,
            IConfiguration configuration,
            WebhookService webhookService,
            KafkaTopicService kafkaTopicService)
        {
            _logger = logger;
            _configuration = configuration;
            _webhookService = webhookService;
            _kafkaTopicService = kafkaTopicService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topicName = _configuration["Kafka:Topic"] ?? "inventory-jobs";
            var consumerGroup = _configuration["Kafka:GroupId"] ?? "inventory-processor";

            _logger.LogInformation($"Kafka Consumer Service is starting. Consumer group: {consumerGroup}, Topic: {topicName}");

            try
            {
                await foreach (var message in _kafkaTopicService.ConsumeMessagesAsync(topicName, consumerGroup, stoppingToken))
                {
                    try
                    {
                        _logger.LogInformation($"Consumed message with key {message.Key} from Kafka topic {topicName}");

                        // Deserialize the message value to an InventoryJob
                        var job = JsonSerializer.Deserialize<InventoryJob>(message.Value);

                        if (job != null)
                        {
                            _logger.LogInformation($"Processing inventory job {job.JobId}");

                            // Check if job already has an error
                            if (job.HasError)
                            {
                                _logger.LogWarning($"Job {job.JobId} has error: {job.ErrorCode} - {job.ErrorMessage}");

                                // Set completion time
                                job.CompletedAt = DateTime.UtcNow;

                                // Notify webhooks about the error
                                await _webhookService.NotifyAllAsync(job);
                            }
                            else
                            {
                                // Simulate long-running process
                                _logger.LogInformation($"Starting long-running inventory processing for job {job.JobId}");
                                await Task.Delay(5000, stoppingToken);
                                _logger.LogInformation($"Completed long-running inventory processing for job {job.JobId}");

                                // Update job status
                                job.Status = "Completed";
                                job.CompletedAt = DateTime.UtcNow;
                                job.Result = "Inventory processing completed successfully";

                                // Notify webhooks
                                await _webhookService.NotifyAllAsync(job);
                            }

                            // Commit the message (in Kafka, this would commit the offset)
                            await _kafkaTopicService.CommitMessageAsync(topicName, consumerGroup, message.Key);

                            _logger.LogInformation($"Completed processing job {job.JobId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing message with key {message.Key}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stoppingToken is canceled
                _logger.LogInformation("Kafka Consumer Service is stopping.");
            }
        }
    }
}
