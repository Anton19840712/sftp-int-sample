using common;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace sftp
{
	public class FileDownloadService : IFileDownloadService
	{
		private readonly SftpConfig _config;
		private readonly ILogger<FileDownloadService> _logger;

		// Инъекция SftpConfig и ILogger
		public FileDownloadService(SftpConfig config, ILogger<FileDownloadService> logger)
		{
			_config = config;
			_logger = logger;
		}

		public async Task DownloadFilesAsync(CancellationToken cancellationToken)
		{
			using (var client = new SftpClient(_config.Host, _config.Port, _config.UserName, _config.Password))
			{
				try
				{
					await client.ConnectAsync(cancellationToken);
					_logger.LogInformation("Подключение к серверу выполнено для скачивания файлов.");

					var files = client.ListDirectory(_config.Source);
					foreach (var file in files)
					{
						if (!file.IsDirectory && !file.IsSymbolicLink)
						{
							var localFilePath = Path.Combine(@"C:\Documents2", file.Name);
							using (var fileStream = File.Create(localFilePath))
							{
								client.DownloadFile(file.FullName, fileStream);
							}
							_logger.LogInformation($"Файл {file.Name} успешно скачан и сохранен в {localFilePath}.");
						}
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"Ошибка при скачивании файлов: {e.Message}");
				}
				finally
				{
					client.Disconnect();
					_logger.LogInformation("Отключение от сервера выполнено после скачивания файлов.");
				}
			}
		}
	}
}
