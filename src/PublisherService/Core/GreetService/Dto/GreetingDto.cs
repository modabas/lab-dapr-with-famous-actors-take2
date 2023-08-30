namespace PublisherService.Core.GreetService.Dto;

public class GreetingDto
{
    public long Id { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
