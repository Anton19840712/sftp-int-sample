using common.abstractions;
using RabbitMQ.Client;

public class FileDownloadService : IHostedService
{
	private readonly IFileProcessorHandler _fileProcessorHandler;
	private readonly ILogger<FileDownloadService> _logger;
	private Timer _timer;

	public FileDownloadService(
		IFileProcessorHandler fileProcessorHandler,
		IConnectionFactory connectionFactory,
		ILogger<FileDownloadService> logger)
	{
		_fileProcessorHandler = fileProcessorHandler;
		_logger = logger;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File Download Service is starting.");
		_timer = new Timer(
			_fileProcessorHandler.ProcessFiles,
			null,
			TimeSpan.Zero,
			TimeSpan.FromSeconds(5));
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File Download Service is stopping.");
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}
}
