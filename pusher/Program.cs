using common.abstractions;
using common.publisher;
using RabbitMQ.Client;
using sftp_int_sample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddLogging();
builder.Services.AddHostedService<FileToQueueService>();
builder.Services.AddTransient<IPublisher, ToQueuePublisher>();
builder.Services.AddTransient<IFileProcessorHandler, FileProcessorPushHandler>();
builder.Services.AddSingleton<IConnectionFactory>(new ConnectionFactory
{
	Uri = new Uri("amqp://service:A1qwert@localhost:5672")
});

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
