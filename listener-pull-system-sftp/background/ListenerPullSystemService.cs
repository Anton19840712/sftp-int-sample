using common.abstractions;
using RabbitMQ.Client;

public class ListenerPullSystemService : BaseListenerService
{
	public ListenerPullSystemService(
		IConnectionFactory connectionFactory,
		ILogger<BaseListenerService> logger)
		: base(connectionFactory, logger) { }

	protected override string QueueName => "sftp-pull-system-queue";
	protected override string LocalDirectory => @"C:\sftp-local-pull-system-listener-storage";
}