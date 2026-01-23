using Microsoft.Extensions.AI;
using Query.Api.Configuration;
using Query.Api.Repositories;
using Query.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Options Configuration
EmbeddingOptions embeddingOpt = new();
builder.Configuration.GetSection("Embedding")
    .Bind(embeddingOpt, opt =>
    {
        opt.ErrorOnUnknownConfiguration = true;
    });

ChatConfigOptions chatOpt = new();

builder.Configuration.GetSection("Chat")
    .Bind(chatOpt, opt =>
    {
        opt.ErrorOnUnknownConfiguration = true;
    });




// AI Configuration

// Register Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(b =>
    b.Use(new OllamaEmbeddingGenerator(new Uri(embeddingOpt.BaseUrl), embeddingOpt.Model)));

// Register Chat Client
builder.Services.AddChatClient(sp =>
{
    return new OllamaChatClient(new Uri(chatOpt.BaseUrl), chatOpt.Model);
});

// Application Services
builder.Services.AddScoped<VectorSearchRepository>();
builder.Services.AddScoped<EmbeddingGeneratorService>();
builder.Services.AddScoped<RagChatService>();
builder.Services.AddScoped<RagFacade>();

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
