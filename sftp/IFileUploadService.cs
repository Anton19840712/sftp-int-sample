namespace sftp
{
	public interface IFileUploadService
	{
		Task UploadFilesAsync(CancellationToken cancellationToken);
	}
}
