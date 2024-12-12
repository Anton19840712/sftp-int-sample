using common;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace sftp
{
	public class FileUploadService : IFileUploadService
	{
		private readonly SftpConfig _config;
		private readonly ILogger<FileUploadService> _logger;

		// Инъекция SftpConfig и ILogger
		public FileUploadService(
			SftpConfig config,
			ILogger<FileUploadService> logger)
		{
			_config = config;
			_logger = logger;
		}

		public async Task UploadFilesAsync(CancellationToken cancellationToken)
		{
			using (var client = new SftpClient(_config.Host, _config.Port, _config.UserName, _config.Password))
			{
				try
				{
					await client.ConnectAsync(cancellationToken);
					_logger.LogInformation("Подключение к серверу выполнено для загрузки файлов.");

					var localDirectory = @"C:\Documents1";
					if (Directory.Exists(localDirectory))
					{
						var files = Directory.GetFiles(localDirectory);

						foreach (var localFilePath in files)
						{
							var remoteFilePath = Path.Combine(_config.Source, Path.GetFileName(localFilePath));
							using (var fileStream = new FileStream(localFilePath, FileMode.Open))
							{
								client.UploadFile(fileStream, remoteFilePath);
							}
							_logger.LogInformation($"Файл {Path.GetFileName(localFilePath)} успешно загружен на сервер в {remoteFilePath}.");
						}
					}
					else
					{
						_logger.LogWarning($"Локальная папка {localDirectory} не найдена.");
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"Ошибка при загрузке файлов: {e.Message}");
				}
				finally
				{
					client.Disconnect();
					_logger.LogInformation("Отключение от сервера выполнено после загрузки файлов.");
				}
			}
		}
	}
}
