using Dapper.FluentMap;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Mapper;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.Service;

public class ApplicationDbContext : ApplicationDbContextBase
{
    public ApplicationDbContext(IOutboxPatternDbContext outboxPatternDbContext) : base(outboxPatternDbContext)
    {
        FluentMapper.Initialize(config =>
        {
            config.AddMap(new GreetingMap());
        });
    }
}
