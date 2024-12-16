using common.abstractions;
using common.models;
using common.publisher;

namespace sftp_int_sample
{
	public class FileProcessorPushHandler : IFileProcessorHandler
	{
		private readonly IPublisher _publisher;
		public readonly ILogger<ToQueuePublisher> _logger;

		public FileProcessorPushHandler(IPublisher publisher, ILogger<ToQueuePublisher> logger)
		{
			_publisher = publisher;
			_logger = logger;
		}

		public void ProcessFiles(object state)
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

					_publisher.PublishToQueue("sftp-push-system-queue", message);

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
	}
}
