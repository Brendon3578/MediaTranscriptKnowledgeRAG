using Microsoft.EntityFrameworkCore;
using Upload.Api.Infrastructure;
using Upload.Api.Infrastructure.Configuration;
using Upload.Api.Infrastructure.FileStorage;
using Upload.Api.Messaging;
using Upload.Api.Services;

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
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Messaging - RabbitMQ
var rabbitMqConfig = builder.Configuration
    .GetSection("RabbitMq")
    .Get<RabbitMqConfiguration>() ?? new RabbitMqConfiguration();

rabbitMqConfig.Validate();

builder.Services.AddSingleton(rabbitMqConfig);


builder.Services.AddSingleton<RabbitMqEventPublisher>();
builder.Services.AddSingleton<IEventPublisher>(sp =>
    sp.GetRequiredService<RabbitMqEventPublisher>());

builder.Services.AddHostedService<RabbitMqHostedService>();


// Services do Controller
builder.Services.AddScoped<IUploadService, LocalUploadService>();



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