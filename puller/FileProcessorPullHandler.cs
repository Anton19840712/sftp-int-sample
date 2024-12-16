using common.abstractions;
using common.configs;
using common.models;
using common.publisher;
using Renci.SshNet;

namespace puller
{
	public class FileProcessorPullHandler : IFileProcessorHandler
	{
		private readonly IPublisher _publisher;
		public readonly ILogger<FileProcessorPullHandler> _logger;
		private readonly SftpConfig _config;
		public FileProcessorPullHandler(
			IPublisher publisher,
			ILogger<FileProcessorPullHandler> logger,
			SftpConfig config)
		{
			_publisher = publisher;
			_logger = logger;
			_config = config;
		}

		public void ProcessFiles(object state)
		{
			using var client = new SftpClient(
				_config.Host,
				_config.Port,
				_config.UserName,
				_config.Password);
			try
			{
				client.Connect();
				_logger.LogInformation("Connected to SFTP server for downloading files.");

				var files = client.ListDirectory(_config.Source);
				foreach (var file in files)
				{
					if (!file.IsDirectory && !file.IsSymbolicLink)
					{
						var localDirectory = @"C:\sftp-local-pull-system-puller-storage";
						if (!Directory.Exists(localDirectory))
						{
							Directory.CreateDirectory(localDirectory);
						}

						var localFilePath = Path.Combine(localDirectory, file.Name);
						using (var fileStream = File.Create(localFilePath))
						{
							client.DownloadFile(file.FullName, fileStream);
						}

						var message = new FileMessage
						{
							FileContent = File.ReadAllBytes(localFilePath),
							FileName = file.Name
						};

						_publisher.PublishToQueue("sftp-pull-system-queue", message);

						File.Delete(localFilePath);
						_logger.LogInformation($"File {file.Name} processed and deleted.");
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
	}
}
