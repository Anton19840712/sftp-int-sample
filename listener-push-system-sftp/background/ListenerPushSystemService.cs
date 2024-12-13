using common;
using RabbitMQ.Client;

public class ListenerPushSystemService : BaseListenerService
{
	public ListenerPushSystemService(
		IConnectionFactory connectionFactory,
		ILogger<BaseListenerService> logger)
		: base(connectionFactory, logger) { }

	protected override string QueueName => "sftp-push-system-queue";
	protected override string LocalDirectory => @"C:\sftp-local-push-system-listener-storage";
}