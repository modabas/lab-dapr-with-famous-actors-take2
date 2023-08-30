namespace PublisherService.Core.Database.OutboxPattern.Dto;

public class OutboxMessageKey
{
    public short OutboxNo { get; set; }
    public DateTime CreatedDate { get; set; }
    public long Position { get; set; }
}
