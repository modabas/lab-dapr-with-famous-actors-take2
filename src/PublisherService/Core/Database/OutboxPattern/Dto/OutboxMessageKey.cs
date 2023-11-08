namespace PublisherService.Core.Database.OutboxPattern.Dto;

public class OutboxMessageKey
{
    public short OutboxNo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Ulid MessageId { get; set; }
}
