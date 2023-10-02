using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using PublisherService.Core.Database.OutboxPattern.Utility;
using Shared.OutboxPattern;

namespace PublisherService.Core.Database.OutboxPattern.OutboxSelector;

public class CorrelationIdOutboxSelector : IOutboxSelector
{
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;

    public CorrelationIdOutboxSelector(IOptions<OutboxPatternOptions> outboxOptions)
    {
        _outboxOptions = outboxOptions;
    }

    public Task<short> DetermineOutboxNo<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var outboxCount = OutboxPatternHelper.GetOutboxCount(_outboxOptions.Value);
        if (outboxCount == 1) { return Task.FromResult((short)0); }

        return Task.FromResult(ConvertGuidToShort(message.CorrelationId, outboxCount));
    }

    private static short ConvertGuidToShort(Guid correlationId, short maxValue)
    {
        byte[] bytes = correlationId.ToByteArray();
        int sum = 0;
        foreach (byte b in bytes)
        {
            sum += b;
        }
        return (short)(sum % maxValue);
    }
}
