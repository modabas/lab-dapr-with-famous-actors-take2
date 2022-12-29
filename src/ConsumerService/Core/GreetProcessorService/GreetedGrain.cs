namespace ConsumerService.Core.GreetProcessorService;

public class GreetedGrain : Grain, IGreetedGrain
{
    private readonly ILogger<GreetedGrain> _logger;

    public GreetedGrain(ILogger<GreetedGrain> logger)
    {
        _logger = logger;
    }

    public Task<bool> Process(string from, string message, GrainCancellationToken grainCancellationToken)
    {
        var to = this.GetPrimaryKeyString();
        _logger.LogInformation("{to} received greeting from {from}. Content is {message}", to, from, message);
        return Task.FromResult(true);
    }
}
