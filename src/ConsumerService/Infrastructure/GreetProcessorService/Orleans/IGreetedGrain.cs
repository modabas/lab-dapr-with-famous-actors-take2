namespace ConsumerService.Infrastructure.GreetProcessorService.Orleans;

public interface IGreetedGrain : IGrainWithStringKey
{
    Task<bool> Process(string from, string message, GrainCancellationToken grainCancellationToken);
}
