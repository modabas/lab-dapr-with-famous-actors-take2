using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.GreetService.Entity;
using PublisherService.Core.GreetService.Service;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.Context;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.Extensions;
using Shared.GreetService.Events;
using Shared.OutboxPattern;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.GreetService.Service;

public class GreetingService : IGreetingService
{
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly ApplicationDbContext _dbContext;

    public GreetingService(IOutboxPublisher outboxPublisher, ApplicationDbContext dbContext)
    {
        _outboxPublisher = outboxPublisher;
        _dbContext = dbContext;
    }

    public async Task<GreetingEntity> CreateGreetingAndEvent(string from, string to, string message, CancellationToken cancellationToken)
    {
        using (var tran = _dbContext.Database.BeginTransaction(_outboxPublisher))
        {
            var entity = new GreetingEntity()
            {
                From = from,
                Message = message,
                To = to
            };
            _dbContext.Greetings.Add(entity);
            var greetingEvent = new GreetingReceived(from, to, message);
            await _outboxPublisher.CreateMessage("take2pubsub", "greetings", new OutboxMessage<GreetingReceived>(greetingEvent), cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tran.CommitAsync(cancellationToken);
            return entity;
        }
    }
}
