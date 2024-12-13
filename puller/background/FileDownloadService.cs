using common;
using RabbitMQ.Client;
using Renci.SshNet;

public class FileDownloadService : BaseFileService, IHostedService
{
	private readonly SftpConfig _config;
	private Timer _timer;

	public FileDownloadService(
		SftpConfig config,
		IConnectionFactory connectionFactory,
		ILogger<FileDownloadService> logger)
		: base(connectionFactory, logger)
	{
		_config = config;
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

					PublishToQueue("sftp-pull-system-queue", message);

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

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File Download Service is stopping.");
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}
}
