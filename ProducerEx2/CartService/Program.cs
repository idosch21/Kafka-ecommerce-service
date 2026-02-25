using CartService.Services;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaConfig>();
builder.Services.AddSingleton(kafkaConfig);
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<OrderRepository>();

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

