namespace PublisherService.Core.Database.OutboxPattern.Service;

public interface IOutboxProcessor
{
    Task Start();
    Task Stop();
}
