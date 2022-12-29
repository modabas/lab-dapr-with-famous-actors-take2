namespace PublisherService.Core.Database.OutboxPattern.Entity;

public class OutboxPrimaryKey
{
    public DateTime CreatedDate { get; set; }
    public long Position { get; set; }
}
