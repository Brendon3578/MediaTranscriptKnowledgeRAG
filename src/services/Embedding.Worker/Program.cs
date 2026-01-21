using MediaEmbedding.Worker;
using MediaEmbedding.Worker.Configuration;
using MediaEmbedding.Worker.Consumers;
using MediaEmbedding.Worker.Data;
using MediaEmbedding.Worker.Embeddings;
using MediaEmbedding.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// Database
var connectionString = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<EmbeddingDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => o.UseVector());
});

// AI / Embeddings
var ollamaUrl = builder.Configuration["Embedding:BaseUrl"] ?? "http://localhost:11434";
var ollamaModel = builder.Configuration["Embedding:Model"] ?? "nomic-embed-text";

// Register Ollama Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b =>
    b.Use(new OllamaEmbeddingGenerator(new Uri(ollamaUrl), ollamaModel)));

// Services
builder.Services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
builder.Services.AddScoped<MediaTranscribedConsumer>();
builder.Services.AddScoped<EmbeddingDataService>();

// Hosted Service (RabbitMQ Listener)
builder.Services.AddHostedService<EmbeddingWorker>();

var host = builder.Build();
host.Run();
