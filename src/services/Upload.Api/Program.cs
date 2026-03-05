using Microsoft.EntityFrameworkCore;
using Upload.Api.Application;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Storage;
using Upload.Api.Configuration;
using Upload.Api.Infrastructure.FileSystem;
using Upload.Api.Infrastructure.Messaging;
using Upload.Api.Infrastructure.Persistence;
using Upload.Api.Infrastructure;
using Upload.Api.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configurações
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Database
builder.Services.AddDbContext<UploadDbContext>(options =>
    options.UseNpgsql(connectionString));

// File Storage
var provider = builder.Configuration["Storage:Provider"];

if (provider == "Minio")
{
    builder.Services.AddSingleton<IFileStorageFacade, MinioFileStorage>();
}
else
{
    builder.Services.AddSingleton<IFileStorageFacade, LocalFileStorage>();
}

// Messaging - RabbitMQ
// Options removed - using IConfiguration directly in services
// Messaging - RabbitMQ
builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq")) // Bind the "RabbitMq" section
    .ValidateDataAnnotations() // Enable validation using data annotations
    .ValidateOnStart();       // Enforce validation at application startup


builder.Services.AddSingleton<RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventPublisher>(sp =>
    sp.GetRequiredService<RabbitMqEventPublisher>());

builder.Services.AddHostedService<RabbitMqHostedService>();

// Services do Controller
builder.Services.AddScoped<IUploadService, UploadMediaUseCase>();
builder.Services.AddScoped<GetMediaStatusUseCase>();
builder.Services.AddScoped<GetTranscriptionSegmentsUseCase>();

// CORS (se necessário) -> TODO: Mudar em uso em produção
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrations automáticas (apenas dev)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UploadDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

//app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
