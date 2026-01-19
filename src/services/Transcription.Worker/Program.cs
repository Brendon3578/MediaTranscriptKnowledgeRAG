using MediaTranscription.Worker;
using Upload.Api.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TranscriptionWorker>();


// Messaging - RabbitMQ
var rabbitMqConfig = builder.Configuration
    .GetSection("RabbitMq")
    .Get<RabbitMqConfiguration>() ?? new RabbitMqConfiguration();

rabbitMqConfig.Validate();

builder.Services.AddSingleton(rabbitMqConfig);



var host = builder.Build();
host.Run();
