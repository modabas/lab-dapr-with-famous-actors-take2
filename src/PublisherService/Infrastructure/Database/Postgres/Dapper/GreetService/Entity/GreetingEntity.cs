namespace PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Entity;

public class GreetingEntity
{
    public long Id { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
