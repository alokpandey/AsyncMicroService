using System.Text.Json;
using InventoryService.Models;

namespace InventoryService.Services
{
    /// <summary>
    /// This service simulates a Kafka producer by using an in-memory queue
    /// In a real-world scenario, this would use the Confluent.Kafka library to produce messages to a Kafka topic
    /// </summary>
    public class KafkaProducerService
    {
        private readonly ILogger<KafkaProducerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly KafkaTopicService _kafkaTopicService;

        public KafkaProducerService(
            ILogger<KafkaProducerService> logger,
            IConfiguration configuration,
            KafkaTopicService kafkaTopicService)
        {
            _logger = logger;
            _configuration = configuration;
            _kafkaTopicService = kafkaTopicService;
        }

        public async Task ProduceAsync(InventoryJob job)
        {
            try
            {
                var topicName = _configuration["Kafka:Topic"] ?? "inventory-jobs";
                _logger.LogInformation($"Producing Kafka message for job {job.JobId} to topic {topicName}");

                // Serialize the job to JSON (simulating Kafka message serialization)
                var messageValue = JsonSerializer.Serialize(job);

                // Publish to the Kafka topic (simulated)
                await _kafkaTopicService.PublishMessageAsync(topicName, job.JobId, messageValue);

                _logger.LogInformation($"Job {job.JobId} published to Kafka topic {topicName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to publish job to Kafka: {ex.Message}");
                throw;
            }
        }
    }
}
