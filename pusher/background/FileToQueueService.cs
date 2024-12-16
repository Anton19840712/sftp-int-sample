using common.abstractions;

public class FileToQueueService : IHostedService
{
	private Timer _timer;

	public readonly IFileProcessorHandler _fileProcessorHandler;
	private readonly ILogger<FileToQueueService> _logger;

	public FileToQueueService(
		IFileProcessorHandler fileProcessorHandler,
		ILogger<FileToQueueService> logger)
	{
		_fileProcessorHandler = fileProcessorHandler;
		_logger = logger;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File to Queue Service is starting.");
		_timer = new Timer(
			_fileProcessorHandler.ProcessFiles,
			null,
			TimeSpan.Zero,
			TimeSpan.FromSeconds(5));
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("File to Queue Service is stopping.");
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}
}
