namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public interface IOutboxTablePartitionCreationService
{
    Task CreatePartitions(CancellationToken cancellationToken);
}
