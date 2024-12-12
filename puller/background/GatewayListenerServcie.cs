using System.Text;
using common;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Renci.SshNet;

namespace rabbit_listener
{
	public class GatewayListenerService : IHostedService
	{
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<GatewayListenerService> _logger;
		private readonly SftpConfig _config;
		private readonly string _localDirectory = @"C:\Documents1";
		private IConnection _connection;
		private IModel _channel;
		private CancellationTokenSource _cts;
		private Task _listenerTask;
		private Task _uploadTask;

		public GatewayListenerService(
			SftpConfig config,
			IConnectionFactory connectionFactory,
			ILogger<GatewayListenerService> logger)
		{
			_config = config;
			_connectionFactory = connectionFactory;
			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			// Создаем локальную директорию, если она отсутствует
			if (!Directory.Exists(_localDirectory))
			{
				Directory.CreateDirectory(_localDirectory);
			}

			// Запускаем задачу для обработки сообщений RabbitMQ
			_listenerTask = Task.Run(() =>
			{
				_connection = _connectionFactory.CreateConnection();
				_channel = _connection.CreateModel();

				_channel.QueueDeclare(
					"sftp_queue",
					durable: true,
					exclusive: false,
					autoDelete: false,
					arguments: null);

				var consumer = new EventingBasicConsumer(_channel);
				consumer.Received += async (model, ea) =>
				{
					try
					{
						var body = ea.Body.ToArray();
						var jsonMessage = Encoding.UTF8.GetString(body);

						// Десериализуем сообщение
						var message = JsonConvert.DeserializeObject<FileMessage>(jsonMessage);
						if (message != null)
						{
							await SaveFileToDiskAsync(message, _cts.Token);
						}

						// Подтверждаем сообщение
						_channel.BasicAck(ea.DeliveryTag, false);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка обработки сообщения.");
					}
				};

				_channel.BasicConsume("sftp_queue", false, consumer);
			}, cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_cts.Cancel();

			await Task.WhenAll(_listenerTask, _uploadTask);

			_channel?.Close();
			_connection?.Close();
		}

		private async Task SaveFileToDiskAsync(FileMessage fileMessage, CancellationToken cancellationToken)
		{
			var fileNameWithExtension = string.IsNullOrWhiteSpace(fileMessage.FileExtension)
				? fileMessage.FileName // Если расширение не указано, сохраняем как есть
				: $"{Path.GetFileNameWithoutExtension(fileMessage.FileName)}.{fileMessage.FileExtension.TrimStart('.')}";

			var filePath = Path.Combine(_localDirectory, fileNameWithExtension);

			try
			{
				await File.WriteAllBytesAsync(filePath, fileMessage.FileContent, cancellationToken);
				_logger.LogInformation($"Файл {fileNameWithExtension} сохранен на диск: {filePath}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Ошибка при сохранении файла {fileNameWithExtension}: {ex.Message}");
			}
		}
	}
}
