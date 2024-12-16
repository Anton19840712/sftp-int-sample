using System.Text;
using common.models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace common.abstractions
{
	public abstract class BaseListenerService : IHostedService
	{
		private readonly IConnectionFactory _connectionFactory;
		private readonly ILogger<BaseListenerService> _logger;
		private IConnection _connection;
		private IModel _channel;
		private CancellationTokenSource _cts;
		private Task _listenerTask;

		protected abstract string QueueName { get; }
		protected abstract string LocalDirectory { get; }

		public BaseListenerService(
			IConnectionFactory connectionFactory,
			ILogger<BaseListenerService> logger)
		{
			_connectionFactory = connectionFactory;
			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			// Создаем локальную директорию, если она отсутствует
			if (!Directory.Exists(LocalDirectory))
			{
				Directory.CreateDirectory(LocalDirectory);
			}

			_listenerTask = Task.Run(() =>
			{
				_connection = _connectionFactory.CreateConnection();
				_channel = _connection.CreateModel();

				_channel.QueueDeclare(
					QueueName,
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

						var message = JsonConvert.DeserializeObject<FileMessage>(jsonMessage);
						if (message != null)
						{
							await SaveFileToDiskAsync(message, _cts.Token);
						}

						_channel.BasicAck(ea.DeliveryTag, false);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Ошибка обработки сообщения.");
					}
				};

				_channel.BasicConsume(QueueName, false, consumer);
			}, cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_cts.Cancel();

			if (_listenerTask != null)
			{
				await _listenerTask;
			}

			_channel?.Close();
			_connection?.Close();
		}

		private async Task SaveFileToDiskAsync(FileMessage fileMessage, CancellationToken cancellationToken)
		{
			var fileNameWithExtension = string.IsNullOrWhiteSpace(fileMessage.FileExtension)
				? fileMessage.FileName
				: $"{Path.GetFileNameWithoutExtension(fileMessage.FileName)}.{fileMessage.FileExtension.TrimStart('.')}";

			var filePath = Path.Combine(LocalDirectory, fileNameWithExtension);

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
