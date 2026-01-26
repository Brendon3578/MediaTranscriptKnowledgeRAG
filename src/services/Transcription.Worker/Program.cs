using MediaTranscription.Worker;
using MediaTranscription.Worker.Application;
using MediaTranscription.Worker.Application.Interfaces;
using MediaTranscription.Worker.Configuration;
using MediaTranscription.Worker.Infrastructure;
using MediaTranscription.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAudioExtractorService, AudioExtractorService>();
builder.Services.AddSingleton<IWhisperModelProvider, WhisperAIModelProvider>();
builder.Services.AddSingleton<IDependencyBootstrapper, WhisperAndFfmpegBootstrapper>();
builder.Services.AddSingleton<ITranscriptionFacade, WhisperNetTranscriptionFacade>();
builder.Services.AddHostedService<TranscriptionWorker>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TranscriptionDbContext>(options =>
    options.UseNpgsql(connectionString));

//services
builder.Services.AddScoped<TranscriptionRepository>();

// Messaging - RabbitMQ
builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq")) // Bind the "RabbitMq" section
    .ValidateDataAnnotations() // Enable validation using data annotations
    .ValidateOnStart();       // Enforce validation at application startup

// other options removed - using IConfiguration directly

var host = builder.Build();

// Executa o bootstrap de dependências nativas antes de iniciar o host
var bootstrapper = host.Services.GetRequiredService<IDependencyBootstrapper>();
await bootstrapper.InitializeAsync(CancellationToken.None);

host.Run();
