using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP client factory
builder.Services.AddHttpClient();

// Register our services
builder.Services.AddSingleton<WebhookService>();
builder.Services.AddSingleton<KafkaTopicService>();
builder.Services.AddSingleton<InventoryProcessingService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<InventoryProcessingService>());

// Configure Kafka settings
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    { "Kafka:BootstrapServers", "localhost:9092" },
    { "Kafka:Topic", "inventory-jobs" },
    { "Kafka:GroupId", "inventory-processor" }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
