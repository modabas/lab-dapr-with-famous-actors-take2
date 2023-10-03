using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Service;

public interface IApplicationDbContext
{
    public DbConnection GetConnection();
}
