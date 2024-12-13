using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHostedService<FileToQueueService>();
builder.Services.AddSingleton<IConnectionFactory>(new ConnectionFactory
{
	Uri = new Uri("amqp://service:A1qwert@localhost:5672")
});

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
