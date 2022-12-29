using Dapper;
using Dapper.FluentMap;
using Microsoft.Extensions.Options;
using Npgsql;
using PublisherService.Core.Database.Config;
using PublisherService.Core.Database.Service;
using PublisherService.Infrastructure.Database.Postgres.Dapper.Mapper;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Service;

internal class DbContext : IDbContext
{
    private readonly IOptionsMonitor<ServiceDbOptions> _dbOptions;

    public DbContext(IOptionsMonitor<ServiceDbOptions> dbOptions)
    {
        _dbOptions = dbOptions;
        FluentMapper.Initialize(config =>
        {
            config.AddMap(new GreetingMap());
            config.AddMap(new OutboxMap());
        });
    }

    public DbConnection GetConnection()
    {
        return new NpgsqlConnection(_dbOptions.CurrentValue.ConnectionString);
    }
}
