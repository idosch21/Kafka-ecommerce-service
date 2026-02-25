using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace CartService.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _orderCreatedTopic;
        private readonly string _orderUpdatedTopic;
        private readonly ILogger<KafkaProducerService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public KafkaProducerService(KafkaConfig kafkaConfig, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaConfig.BootstrapServers,
                EnableIdempotence = true,
                Acks = Acks.All
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _orderCreatedTopic = kafkaConfig.OrderCreatedTopic;
            _orderUpdatedTopic = kafkaConfig.OrderUpdatedTopic;

            _retryPolicy = Policy
                .Handle<ProduceException<string, string>>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Attempt {retryCount} failed to produce message. Retrying in {timeSpan.TotalSeconds} seconds. Error: {exception.Message}");
                    }
                );
        }

        public async Task<bool> ProduceOrderCreatedEvent(string key, object order)
        {
            _logger.LogInformation($"Attempting to produce OrderCreated event: Key = {key}");
            return await ProduceAsync(_orderCreatedTopic, key, order);
        }

        public async Task<bool> ProduceOrderUpdatedEvent(string key, object order)
        {
            _logger.LogInformation($"Attempting to produce OrderUpdated event: Key = {key}");
            return await ProduceAsync(_orderUpdatedTopic, key, order);
        }

        private async Task<bool> ProduceAsync(string topic, string key, object value)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = JsonConvert.SerializeObject(value)
                };

                var result = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var deliveryResult = await _producer.ProduceAsync(topic, message);
                    _logger.LogInformation($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
                    return deliveryResult;
                });

                return true;
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError($"Failed to deliver message after retries: {e.Error.Reason}");
                return false;
            }
        }
    }
}
