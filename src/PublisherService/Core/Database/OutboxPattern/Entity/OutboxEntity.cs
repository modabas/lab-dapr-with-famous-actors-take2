namespace PublisherService.Core.Database.OutboxPattern.Entity;

public class OutboxEntity
{
    public long Position { get; set; }
    public Guid MessageId { get; set; }
    public string PubSubName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}
