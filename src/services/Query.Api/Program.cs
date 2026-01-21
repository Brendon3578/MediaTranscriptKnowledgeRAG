using Microsoft.Extensions.AI;
using Query.Api.Repositories;
using Query.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// AI Configuration
var ollamaUrl = builder.Configuration["Embedding:BaseUrl"] ?? "http://localhost:11434";
var embeddingModel = builder.Configuration["Embedding:Model"] ?? "nomic-embed-text";
var chatModel = builder.Configuration["Chat:Model"] ?? "llama3";

// Register Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b =>
    b.Use(new OllamaEmbeddingGenerator(new Uri(ollamaUrl), embeddingModel)));

// Register Chat Client
builder.Services.AddChatClient(sp => {
    return new OllamaChatClient(new Uri(ollamaUrl), chatModel);
});

// Application Services
builder.Services.AddScoped<VectorSearchRepository>();
builder.Services.AddScoped<EmbeddingQueryService>();
builder.Services.AddScoped<RagChatService>();
builder.Services.AddScoped<RagFacade>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
