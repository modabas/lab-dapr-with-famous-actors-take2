using PublisherService.Core.GreetService.Dto;
using PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Entity;

namespace PublisherService.Infrastructure.Database.Postgres.Dapper.GreetService.Extensions;

public static class GreetingEntityExtensions
{
    public static GreetingDto ToDto(this GreetingEntity entity)
    {
        return new GreetingDto
        {
            Id = entity.Id,
            From = entity.From,
            To = entity.To,
            Message = entity.Message
        };
    }
}
