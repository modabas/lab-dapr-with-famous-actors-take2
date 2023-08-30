using PublisherService.Core.Database.OutboxPattern.Dto;
using Shared.OutboxPattern;
using System.Data.Common;

namespace PublisherService.Core.Database.OutboxPattern.Service;

public interface IOutboxPublisher
{
    public DbTransaction? DbTransaction { get; set; }

    Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken);
    Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, Guid correlationId, OutboxMessage<TMessage> message, CancellationToken cancellationToken);
}
