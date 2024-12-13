using System.Text;
using common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Renci.SshNet;

namespace puller.background
{
	/// <summary>
	/// Данный сервис обращается на sftp и приземляя оттуда данные на диск, отправляет их в сетевую очередь.
	/// </summary>
	public class FileDownloadService : IHostedService
	{
		private readonly SftpConfig _config;
		private readonly ILogger<FileDownloadService> _logger;
		private readonly IConnectionFactory _connectionFactory;
		private Timer _timer;

		public FileDownloadService(
			SftpConfig config,
			ILogger<FileDownloadService> logger,
			IConnectionFactory connectionFactory)
		{
			_config = config;
			_logger = logger;
			_connectionFactory = connectionFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("File Download Service is starting.");

			_timer = new Timer(ProcessFiles, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

			return Task.CompletedTask;
		}

		private void ProcessFiles(object state)
		{
			using var client = new SftpClient(_config.Host, _config.Port, _config.UserName, _config.Password);
			try
			{
				client.Connect();
				_logger.LogInformation("Connected to SFTP server for downloading files.");

				var files = client.ListDirectory(_config.Source);
				foreach (var file in files)
				{
					if (!file.IsDirectory && !file.IsSymbolicLink)
					{
						// Проверка и создание директории
						var localDirectory = @"C:\sftp-local-pull-system-puller-storage";
						if (!Directory.Exists(localDirectory))
						{
							Directory.CreateDirectory(localDirectory);
						}

						var localFilePath = Path.Combine(localDirectory, file.Name);

						_logger.LogInformation($"Attempting to save file to {localFilePath}");

						using (var fileStream = File.Create(localFilePath))
						{
							client.DownloadFile(file.FullName, fileStream);
						}
						_logger.LogInformation($"File {file.Name} successfully downloaded to {localFilePath}.");

						PublishToQueue("sftp-pull-system-queue", localFilePath, file.Name);

						File.Delete(localFilePath);
						_logger.LogInformation($"File {file.Name} processed and deleted from local disk.");
					}
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Error while downloading files: {Message}", e.Message);
			}
			finally
			{
				client.Disconnect();
				_logger.LogInformation("Disconnected from SFTP server.");
			}
		}

		private void PublishToQueue(string queueName, string filePath, string fileName)
		{
			using var connection = _connectionFactory.CreateConnection();
			using var channel = connection.CreateModel();

			channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

			var fileContent = File.ReadAllBytes(filePath);

			var message = new FileMessage
			{
				FileContent = fileContent,
				FileName = fileName
			};

			var jsonMessage = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: null,
				body: body
			);

			_logger.LogInformation("Message added to queue {QueueName} for file {FileName}", queueName, fileName);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("File Download Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}
	}
}


