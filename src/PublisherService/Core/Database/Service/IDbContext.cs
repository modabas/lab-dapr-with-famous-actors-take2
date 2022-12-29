using System.Data.Common;

namespace PublisherService.Core.Database.Service;

public interface IDbContext
{
    public DbConnection GetConnection();
}