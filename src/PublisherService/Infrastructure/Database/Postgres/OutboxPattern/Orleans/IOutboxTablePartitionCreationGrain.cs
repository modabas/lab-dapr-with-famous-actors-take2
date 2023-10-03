using Orleans.Runtime;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public interface IOutboxTablePartitionCreationGrain : IGrainWithIntegerKey
{
    Task<IGrainReminder> RegisterOrUpdateReminder();
}