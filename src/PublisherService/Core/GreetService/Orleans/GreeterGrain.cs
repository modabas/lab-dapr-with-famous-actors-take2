using PublisherService.Core.GreetService.Service;

namespace PublisherService.Core.GreetService.Orleans;

public class GreeterGrain : Grain, IGreeterGrain
{
    private readonly ILogger<GreeterGrain> _logger;
    private readonly IGreetingService _greetingRepo;

    public GreeterGrain(ILogger<GreeterGrain> logger, IGreetingService greetingRepo)
    {
        _logger = logger;
        _greetingRepo = greetingRepo;
    }

    public async Task<bool> Send(string to, string message, GrainCancellationToken grainCancellationToken)
    {
        var from = this.GetPrimaryKeyString();
        await _greetingRepo.CreateGreetingAndEvent(from, to, message, grainCancellationToken.CancellationToken);
        _logger.LogInformation("Published data. From: {from}, To: {to}, Message: {message}", from, to, message);
        return true;
    }
}
