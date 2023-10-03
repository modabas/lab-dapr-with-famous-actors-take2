using PublisherService.Core.Database.OutboxPattern.Service;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Service;

public abstract class ApplicationDbContextBase : IApplicationDbContext
{
    protected readonly IOutboxPatternDbContext _outboxPatternDbContext;

    public ApplicationDbContextBase(IOutboxPatternDbContext outboxPatternDbContext)
    {
        _outboxPatternDbContext = outboxPatternDbContext;
    }

    public DbConnection GetConnection()
    {
        return _outboxPatternDbContext.GetConnection();
    }
}
