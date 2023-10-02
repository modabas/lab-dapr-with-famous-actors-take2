using Dapper.FluentMap.Mapping;
using PublisherService.Core.GreetService.Entity;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Mapper;

public class GreetingMap : EntityMap<GreetingEntity>
{
    public GreetingMap()
    {
        Map(p => p.Id).ToColumn("id");
        Map(p => p.From).ToColumn("from");
        Map(p => p.To).ToColumn("to");
        Map(p => p.Message).ToColumn("message");
    }
}