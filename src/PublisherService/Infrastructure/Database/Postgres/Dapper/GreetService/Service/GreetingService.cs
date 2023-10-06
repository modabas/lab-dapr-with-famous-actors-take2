using Dapper;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.GreetService.Entity;
using PublisherService.Core.GreetService.Service;
using PublisherService.Infrastructure.Database.Postgres.Dapper.Context;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Extensions;
using Shared.GreetService.Events;
using Shared.OutboxPattern;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Service;

public class GreetingService : IGreetingService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOutboxWriter _outboxPublisher;

    public GreetingService(IApplicationDbContext dbContext, IOutboxWriter outboxPublisher)
    {
        _dbContext = dbContext;
        _outboxPublisher = outboxPublisher;
    }

    public async Task<GreetingEntity> CreateGreetingAndEvent(string from, string to, string message, CancellationToken cancellationToken)
    {
        using (var conn = _dbContext.GetConnection())
        {
            await conn.OpenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using (var tran = conn.BeginTransaction(_outboxPublisher))
            {
                var sql = "INSERT INTO tbl_greeting (\"from\", \"to\", message) VALUES (@from, @to, @message) RETURNING *;";
                var entity = await conn.QuerySingleOrDefaultAsync<GreetingEntity>(new CommandDefinition(sql, new { from, to, message }, tran, cancellationToken: cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();

                if (entity is null)
                    throw new ApplicationException("Cannot create greeting db entry.");

                var greetingEvent = new GreetingReceived(from, to, message);
                await _outboxPublisher.CreateMessage("take2pubsub", "greetings", new OutboxMessage<GreetingReceived>(greetingEvent), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                await tran.CommitAsync(cancellationToken);
                return entity;
            }
        }
    }
}
