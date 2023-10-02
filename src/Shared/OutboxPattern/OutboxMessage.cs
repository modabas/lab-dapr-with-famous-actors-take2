namespace Shared.OutboxPattern;

public class OutboxMessage<TMessage>
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public TMessage? Message { get; set; }

    public OutboxMessage()
    {

    }

    public OutboxMessage(TMessage message) : this(Guid.NewGuid(), Guid.NewGuid(), message) { }

    public OutboxMessage(Guid id, Guid correlationId, TMessage message)
    {
        Id = id;
        CorrelationId = correlationId;
        Message = message;
    }
}
