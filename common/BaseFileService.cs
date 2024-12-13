using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace common
{
	public abstract class BaseFileService
	{
		protected readonly ILogger _logger;
		private readonly IConnectionFactory _connectionFactory;

		protected BaseFileService(IConnectionFactory connectionFactory, ILogger logger)
		{
			_connectionFactory = connectionFactory;
			_logger = logger;
		}

		protected void PublishToQueue(string queueName, FileMessage message)
		{
			using var connection = _connectionFactory.CreateConnection();
			using var channel = connection.CreateModel();

			channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

			var jsonMessage = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: null,
				body: body
			);

			_logger.LogInformation("Message added to queue {QueueName} for file {FileName}", queueName, message.FileName);
		}
	}
}