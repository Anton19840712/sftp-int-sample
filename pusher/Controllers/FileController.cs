using common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

public class FileController : ControllerBase
{
	private readonly IConnectionFactory _connectionFactory;
	private readonly ILogger<FileController> _logger;

	public FileController(
		IConnectionFactory connectionFactory,
		ILogger<FileController> logger)
	{
		_connectionFactory = connectionFactory;
		_logger = logger;
	}

	[HttpPost("upload")]
	public async Task<IActionResult> UploadFile(IFormFile file)
	{
		try
		{
			using var stream = new MemoryStream();
			await file.CopyToAsync(stream);
			byte[] fileContent = stream.ToArray();

			string fileExtension = Path.GetExtension(file.FileName);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);

			// Передаем имя файла
			PublishToQueue(
				"sftp_queue",
				fileContent,
				fileExtension,
				fileNameWithoutExtension);

			return Ok("Файл успешно загружен и передан на обработку.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при загрузке файла.");
			return StatusCode(500, "Произошла ошибка при обработке файла.");
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

		{
			// Убедимся, что очередь существует
			channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

			// Создаем объект с метаинформацией и содержимым файла
			var message = new FileMessage
			{
				FileContent = fileContent,  // Бинарное содержимое файла
				FileExtension = fileExtension,
				FileName = fileNameWithoutExtension // Передаем имя файла
			};

			// Сериализуем объект в JSON
			var jsonMessage = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			// Публикуем сообщение в очередь RabbitMQ
			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: null,  // без заголовков
				body: body
			);
		}

		_logger.LogInformation("Сообщение добавлено в очередь {QueueName}", queueName);
	}
}
