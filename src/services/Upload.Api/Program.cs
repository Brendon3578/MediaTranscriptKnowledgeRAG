using Microsoft.EntityFrameworkCore;
using Upload.Api.Application;
using Upload.Api.Application.Interfaces;
using Upload.Api.Configuration;
using Upload.Api.Infrastructure.FileSystem;
using Upload.Api.Infrastructure.Menssaging;
using Upload.Api.Infrastructure.Persistence;

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
builder.Services.AddSingleton<IFileStorageFacade, LocalFileStorage>();

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

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.UseHttpsRedirection();

app.Run();
