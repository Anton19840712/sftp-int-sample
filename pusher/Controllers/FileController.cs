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

			// �������� ��� �����
			PublishToQueue(
				"sftp_queue",
				fileContent,
				fileExtension,
				fileNameWithoutExtension);

			return Ok("���� ������� �������� � ������� �� ���������.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "������ ��� �������� �����.");
			return StatusCode(500, "��������� ������ ��� ��������� �����.");
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
			// ��������, ��� ������� ����������
			channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

			// ������� ������ � ��������������� � ���������� �����
			var message = new FileMessage
			{
				FileContent = fileContent,  // �������� ���������� �����
				FileExtension = fileExtension,
				FileName = fileNameWithoutExtension // �������� ��� �����
			};

			// ����������� ������ � JSON
			var jsonMessage = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			// ��������� ��������� � ������� RabbitMQ
			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: null,  // ��� ����������
				body: body
			);
		}

		_logger.LogInformation("��������� ��������� � ������� {QueueName}", queueName);
	}
}