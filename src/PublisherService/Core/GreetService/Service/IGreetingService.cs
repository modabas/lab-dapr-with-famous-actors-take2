using PublisherService.Core.GreetService.Dto;

namespace PublisherService.Core.GreetService.Service;

public interface IGreetingService
{
    Task<GreetingDto> CreateGreetingAndEvent(string from, string to, string message, CancellationToken cancellationToken);
}
