using PublisherService.Core.Database.OutboxPattern.Entity;
using Shared.OutboxPattern;
using System.Data.Common;

namespace PublisherService.Core.Database.OutboxPattern.Service;

public interface IOutboxPublisher
{
    public DbTransaction? DbTransaction { get; set; }

    Task<OutboxPrimaryKey> CreateMessage<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken);
    Task<OutboxPrimaryKey> CreateMessage<TMessage>(string pubSubName, string topicName, Guid correlationId, OutboxMessage<TMessage> message, CancellationToken cancellationToken);
}
