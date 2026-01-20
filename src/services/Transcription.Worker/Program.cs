using MediaTranscription.Worker;
using MediaTranscription.Worker.Facade;
using MediaTranscription.Worker.Infrastructure.Configuration;
using MediaTranscription.Worker.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAudioExtractorService, AudioExtractorService>();
builder.Services.AddSingleton<IWhisperModelProvider, WhisperGgmlModelProvider>();
builder.Services.AddSingleton<IDependencyBootstrapper, WhisperAndFfmpegBootstrapper>();
builder.Services.AddSingleton<ITranscriptionFacade, WhisperNetTranscriptionFacade>();
builder.Services.AddHostedService<TranscriptionWorker>();


// Messaging - RabbitMQ
builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq")) // Bind the "RabbitMq" section
    .ValidateDataAnnotations() // Enable validation using data annotations
    .ValidateOnStart();       // Enforce validation at application startup

builder.Services.AddOptions<WhisperOptions>()
    .Bind(builder.Configuration.GetSection("Whisper"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<FFmpegOptions>()
    .Bind(builder.Configuration.GetSection("FFmpeg"))
    .ValidateDataAnnotations()
    .ValidateOnStart();


var host = builder.Build();

// Executa o bootstrap de dependências nativas antes de iniciar o host
var bootstrapper = host.Services.GetRequiredService<IDependencyBootstrapper>();
await bootstrapper.InitializeAsync(CancellationToken.None);

host.Run();
