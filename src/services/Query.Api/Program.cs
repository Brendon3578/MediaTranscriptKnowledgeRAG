using Microsoft.Extensions.AI;
using Query.Api.Application;
using Query.Api.Infrastructure;
using Query.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// AI Configuration
var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";

var embeddingBaseUrl = builder.Configuration["Embedding:BaseUrl"] ?? ollamaBaseUrl;
var embeddingModel = builder.Configuration["Embedding:Model"] ?? "bge-m3";

// Register Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b =>
    b.Use(new OllamaEmbeddingGenerator(new Uri(embeddingBaseUrl), embeddingModel)));

var chatBaseUrl = builder.Configuration["Chat:BaseUrl"] ?? ollamaBaseUrl;
var chatModel = builder.Configuration["Chat:Model"] ?? "phi3:mini";

// Register Chat Client
builder.Services.AddChatClient(sp =>
{
    return new OllamaChatClient(new Uri(chatBaseUrl), chatModel);
});

// Application Services
builder.Services.AddScoped<TranscriptionSegmentVectorSearchRepository>();
builder.Services.AddScoped<EmbeddingGeneratorService>();
builder.Services.AddScoped<GenerateAnswerUseCase>();
builder.Services.AddScoped<RagFacade>();
builder.Services.AddHttpClient<IOllamaHealthCheckService, OllamaHealthCheckService>();

var app = builder.Build();

// Ensure Ollama is reachable before serving requests.
using (var scope = app.Services.CreateScope())
{
    var healthCheck = scope.ServiceProvider.GetRequiredService<IOllamaHealthCheckService>();
    await healthCheck.CheckAvailabilityAsync(CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
