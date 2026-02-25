using Confluent.Kafka;
using Newtonsoft.Json;
using OrderService.Models;
using Polly;
using Polly.Retry;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrderService.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly OrderRepository _orderRepository;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly KafkaConfig _kafkaConfig;

        public KafkaConsumerService(KafkaConfig kafkaConfig, ILogger<KafkaConsumerService> logger, OrderRepository orderRepository)
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _kafkaConfig = kafkaConfig;

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaConfig.BootstrapServers,
                GroupId = kafkaConfig.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                MaxPollIntervalMs = 300000,
                SessionTimeoutMs = 10000
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Attempt {retryCount} failed. Retrying in {timeSpan.TotalSeconds} seconds. Error: {exception.Message}");
                    }
                );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to both topics.
            _consumer.Subscribe(new[] { _kafkaConfig.OrderCreatedTopic, _kafkaConfig.OrderUpdatedTopic });
            _logger.LogInformation("KafkaConsumerService started and subscribed to topics");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        if (consumeResult != null)
                        {
                            _logger.LogInformation($"Consumed message from {consumeResult.Topic}: {consumeResult.Message.Value}");
                            await ProcessMessage(consumeResult);
                            _consumer.Commit(consumeResult);
                            _logger.LogInformation($"Processed and committed offset: {consumeResult.TopicPartitionOffset}");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error processing message: {e.Message}");
                }
            }

            _logger.LogInformation("KafkaConsumerService stopping");
            _consumer.Close();
        }

        private Task ProcessMessage(ConsumeResult<string, string> consumeResult)
        {
            try
            {
                var order = JsonConvert.DeserializeObject<Order>(consumeResult.Message.Value);
                if (order != null)
                {
                    // Set status based on topic
                    if (consumeResult.Topic == _kafkaConfig.OrderCreatedTopic)
                    {
                        order.Status = "new";
                        _logger.LogInformation($"Processing new order: {order.OrderId}");
                    }
                    else if (consumeResult.Topic == _kafkaConfig.OrderUpdatedTopic)
                    {
                        //order.Status = "updated";
                        _logger.LogInformation($"Processing updated order: {order.OrderId}");
                    }

                    _orderRepository.AddOrUpdateOrder(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessMessage: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    }
}
