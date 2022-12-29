namespace PublisherService.Core.Database.OutboxPattern.Orleans;

public interface IOutboxProcessor
{
    Task Start();
    Task Stop();
}
