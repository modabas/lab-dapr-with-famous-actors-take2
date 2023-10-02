using Microsoft.Extensions.Options;
using PublisherService.Core.Database.OutboxPattern.Utility;
using Shared.OutboxPattern;
using System.Security.Cryptography;

namespace PublisherService.Core.Database.OutboxPattern.OutboxSelector;

public class RandomOutboxSelector : IOutboxSelector
{
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;

    public RandomOutboxSelector(IOptions<OutboxPatternOptions> outboxOptions)
    {
        _outboxOptions = outboxOptions;
    }

    public Task<short> DetermineOutboxNo<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var outboxCount = OutboxPatternHelper.GetOutboxCount(_outboxOptions.Value);
        if (outboxCount == 1) { return Task.FromResult((short)0); }

        return Task.FromResult((short)RandomNumberGenerator.GetInt32(outboxCount));
    }
}
