namespace PublisherService.Core.Database.OutboxPattern.Orleans;

public interface IOutboxTablePartitionCreationGrain : IGrainWithIntegerKey
{
    Task<bool> IsBusy();
    Task Poke();
}