using System.Collections.Concurrent;
using System.Threading.Channels;

namespace InventoryService.Services
{
    /// <summary>
    /// This service simulates Kafka topics using in-memory channels
    /// In a real-world scenario, this would be replaced by actual Kafka topics
    /// </summary>
    public class KafkaTopicService
    {
        private readonly ILogger<KafkaTopicService> _logger;
        private readonly ConcurrentDictionary<string, Channel<KafkaMessage>> _topics = new();

        public KafkaTopicService(ILogger<KafkaTopicService> logger)
        {
            _logger = logger;
        }

        public async Task PublishMessageAsync(string topicName, string key, string value)
        {
            var topic = GetOrCreateTopic(topicName);
            var message = new KafkaMessage(key, value);
            
            await topic.Writer.WriteAsync(message);
            _logger.LogInformation($"Message with key {key} published to topic {topicName}");
        }

        public IAsyncEnumerable<KafkaMessage> ConsumeMessagesAsync(string topicName, string consumerGroup, CancellationToken cancellationToken)
        {
            var topic = GetOrCreateTopic(topicName);
            _logger.LogInformation($"Consumer group {consumerGroup} subscribed to topic {topicName}");
            
            return topic.Reader.ReadAllAsync(cancellationToken);
        }

        public async Task CommitMessageAsync(string topicName, string consumerGroup, string messageKey)
        {
            // In a real Kafka implementation, this would commit the offset for the consumer group
            // For our simulation, we'll just log it
            _logger.LogInformation($"Consumer group {consumerGroup} committed message with key {messageKey} in topic {topicName}");
            await Task.CompletedTask;
        }

        private Channel<KafkaMessage> GetOrCreateTopic(string topicName)
        {
            return _topics.GetOrAdd(topicName, _ => 
            {
                _logger.LogInformation($"Creating Kafka topic: {topicName}");
                return Channel.CreateUnbounded<KafkaMessage>();
            });
        }
    }

    public record KafkaMessage(string Key, string Value);
}
