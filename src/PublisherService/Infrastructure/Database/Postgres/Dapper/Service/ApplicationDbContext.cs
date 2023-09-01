using Dapper.FluentMap;
using Microsoft.Extensions.Options;
using PublisherService.Core.Database.Config;
using PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Mapper;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Service;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(IOptionsMonitor<ServiceDbOptions> dbOptions) : base(dbOptions)
    {
        FluentMapper.Initialize(config =>
        {
            config.AddMap(new GreetingMap());
        });
    }
}
