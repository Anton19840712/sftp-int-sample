using common;
using RabbitMQ.Client;

public class FileToQueueService : BaseFileService, IHostedService
{
	private Timer _timer;

	public FileToQueueService(
		IConnectionFactory connectionFactory,
		ILogger<FileToQueueService> logger)
		: base(connectionFactory, logger) { }

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File to Queue Service is starting.");
		_timer = new Timer(ProcessFiles, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
		return Task.CompletedTask;
	}

	private void ProcessFiles(object state)
	{
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
				var fileName = Path.GetFileName(localFilePath);

				var message = new FileMessage
				{
					FileContent = fileContent,
					FileExtension = fileExtension,
					FileName = fileName
				};

				PublishToQueue("sftp-push-system-queue", message);

				// Optionally delete the file after processing
				// File.Delete(localFilePath);

				_logger.LogInformation($"File {localFilePath} processed.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error processing file {localFilePath}: {ex.Message}");
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File to Queue Service is stopping.");
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}
}
