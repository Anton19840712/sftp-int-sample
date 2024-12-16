using System.Text;
using common.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace common.publisher
{
	public class ToQueuePublisher : IPublisher
	{
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<ToQueuePublisher> _logger;

		public ToQueuePublisher(IConnectionFactory connectionFactory, ILogger<ToQueuePublisher> logger)
		{
			_connectionFactory = connectionFactory;
			_logger = logger;
		}
		public void PublishToQueue(
			string queueName,
			FileMessage message)
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
