using MediaEmbedding.Worker;
using MediaEmbedding.Worker.Application.Interfaces;
using MediaEmbedding.Worker.Application.UseCases;
using MediaEmbedding.Worker.Infrastructure.AI;
using MediaEmbedding.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
// Removed Options configuration

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
builder.Services.AddScoped<GenerateEmbeddingUseCase>();

// Hosted Service (RabbitMQ Listener)
builder.Services.AddHostedService<EmbeddingWorker>();

var host = builder.Build();
host.Run();
