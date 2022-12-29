namespace ConsumerService.Core.GreetProcessorService;

public interface IGreetedGrain : IGrainWithStringKey
{
    Task<bool> Process(string from, string message, GrainCancellationToken grainCancellationToken);
}
