namespace CartService.Services
{
    public class KafkaConfig
    {
        public string BootstrapServers { get; set; }
        public string OrderCreatedTopic { get; set; }
        public string OrderUpdatedTopic { get; set; }
    }
}
