using OrderService.Services;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Determine the environment
var isDevelopment = builder.Environment.IsDevelopment();

// Configure Kafka
var kafkaConfig = new KafkaConfig
{
    BootstrapServers = isDevelopment ? "localhost:9093" : builder.Configuration["Kafka:BootstrapServers"],
    OrderCreatedTopic = builder.Configuration["Kafka:OrderCreatedTopic"],
    OrderUpdatedTopic = builder.Configuration["Kafka:OrderUpdatedTopic"],
    ConsumerGroupId = builder.Configuration["Kafka:ConsumerGroupId"]
};

builder.Services.AddSingleton(kafkaConfig);
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();


//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => "OK");

app.Run();

