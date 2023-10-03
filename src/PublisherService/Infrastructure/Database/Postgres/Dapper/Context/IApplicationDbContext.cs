using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Context;

public interface IApplicationDbContext
{
    public DbConnection GetConnection();
}
