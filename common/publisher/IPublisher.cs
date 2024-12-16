using common.models;

namespace common.publisher
{
	public interface IPublisher
	{
		void PublishToQueue(
			string queueName,
			FileMessage message);
	}
}
