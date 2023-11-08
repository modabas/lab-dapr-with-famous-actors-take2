namespace Shared.OutboxPattern;

public class OutboxMessage<TMessage>
{
    public Ulid Id { get; set; }
    public Ulid CorrelationId { get; set; }
    public TMessage? Message { get; set; }

    public OutboxMessage()
    {

    }

    public OutboxMessage(TMessage message) : this(Ulid.NewUlid(), Ulid.NewUlid(), message) { }

    public OutboxMessage(Ulid id, Ulid correlationId, TMessage message)
    {
        Id = id;
        CorrelationId = correlationId;
        Message = message;
    }
}
