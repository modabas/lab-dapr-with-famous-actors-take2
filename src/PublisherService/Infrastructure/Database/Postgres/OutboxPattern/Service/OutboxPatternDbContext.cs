using Microsoft.Extensions.Options;
using Npgsql;
using PublisherService.Core.Database.Config;
using PublisherService.Core.Database.OutboxPattern.Service;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxPatternDbContext : IOutboxPatternDbContext
{
    private readonly IOptionsMonitor<ServiceDbOptions> _dbOptions;

    public OutboxPatternDbContext(IOptionsMonitor<ServiceDbOptions> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public DbConnection GetConnection()
    {
        return new NpgsqlConnection(_dbOptions.CurrentValue.ConnectionString);
    }
}
