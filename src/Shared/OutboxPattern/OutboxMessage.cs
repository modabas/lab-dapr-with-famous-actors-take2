namespace Shared.OutboxPattern;

public class OutboxMessage<TMessage>
{
    public Guid Id { get; set; }
    public TMessage? Message { get; set; }

    public OutboxMessage()
    {

    }

    public OutboxMessage(TMessage message) : this(Guid.NewGuid(), message) { }

    public OutboxMessage(Guid id, TMessage message)
    {
        Id = id;
        Message = message;
    }
}
