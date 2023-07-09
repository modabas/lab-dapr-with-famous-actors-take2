namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Utility;

public class OutboxPatternOptions
{
    public short OutboxCount { get; set; } = 1;

    public string SchemaName { get; set; } = "outbox_pattern";
}

