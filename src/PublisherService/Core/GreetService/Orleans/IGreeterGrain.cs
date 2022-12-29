using Orleans;

namespace PublisherService.Core.GreetService.Orleans;

public interface IGreeterGrain : IGrainWithStringKey
{
    Task<bool> Send(string to, string message, GrainCancellationToken grainCancellationToken);
}
