using PublisherService.Core.GreetService.Entity;

namespace PublisherService.Core.GreetService.Service;

public interface IGreetingRepo
{
    Task<GreetingEntity> CreateGreetingAndEvent(string from, string to, string message, CancellationToken cancellationToken);
}
