namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public interface IOutboxTablePartitionCreationGrain : IGrainWithIntegerKey
{
    Task<bool> IsBusy();
    Task Poke();
}