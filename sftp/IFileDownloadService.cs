namespace sftp
{
	public interface IFileDownloadService
	{
		Task DownloadFilesAsync(CancellationToken cancellationToken);
	}
}
