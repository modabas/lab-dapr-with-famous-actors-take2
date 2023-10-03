using System.Data.Common;

namespace PublisherService.Core.Database.OutboxPattern.Service;

public interface IOutboxPatternDbContext
{
    public DbConnection GetConnection();
}