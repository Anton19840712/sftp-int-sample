using System.Text;
using common;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace sftp_int_sample.background
{
	public class FileToQueueService : IHostedService
	{
		private readonly ILogger<FileToQueueService> _logger;
		private readonly IConnectionFactory _connectionFactory;
		private Timer _timer;

		public FileToQueueService(
			IConnectionFactory connectionFactory,
			ILogger<FileToQueueService> logger)
		{
			_connectionFactory = connectionFactory;
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("File to Queue Service is starting.");

			_timer = new Timer(ProcessFiles, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

			return Task.CompletedTask;
		}

		private void ProcessFiles(object state)
		{
			// предполагается, что где-то на диске пользователя находятся файлы, которые нужно подгрузить на sftp server
			var localDirectory = @"C:\test-data";

			if (!Directory.Exists(localDirectory))
			{
				_logger.LogWarning($"Local directory {localDirectory} not found.");
				return;
			}

			var files = Directory.GetFiles(localDirectory);

			foreach (var localFilePath in files)
			{
				try
				{
					var fileContent = File.ReadAllBytes(localFilePath);
					var fileExtension = Path.GetExtension(localFilePath);
					var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(localFilePath);

					PublishToQueue(
						"sftp-push-system-queue",
						fileContent,
						fileExtension,
						fileNameWithoutExtension
					);

					// Optionally delete the file after processing
					// File.Delete(localFilePath);
					_logger.LogInformation($"File {localFilePath} processed and deleted.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error processing file {localFilePath}: {ex.Message}");
				}
			}
		}

		private void PublishToQueue(
			string queueName,
			byte[] fileContent,
			string fileExtension,
			string fileNameWithoutExtension)
		{
			using var connection = _connectionFactory.CreateConnection();
			using var channel = connection.CreateModel();

			channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

			var message = new FileMessage
			{
				FileContent = fileContent,
				FileExtension = fileExtension,
				FileName = fileNameWithoutExtension
			};

			var jsonMessage = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: null,
				body: body
			);

			_logger.LogInformation("Message added to queue {QueueName}", queueName);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("File to Queue Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}
	}
}