using Shared.OutboxPattern;

namespace PublisherService.Core.Database.OutboxPattern.OutboxSelector;

public interface IOutboxSelector
{
    Task<short> DetermineOutboxNo<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken);
}
