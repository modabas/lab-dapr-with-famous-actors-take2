using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.TypeMaps;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Extensions;

public static class DbContextExtensions
{
    public static void AddMap<TEntity>(this DbContext dbContext, IEntityMap<TEntity> mapper) where TEntity : class
    {
        if (FluentMapper.EntityMaps.TryAdd(typeof(TEntity), mapper))
        {
            SqlMapper.SetTypeMap(typeof(TEntity), new FluentMapTypeMap<TEntity>());
            return;
        }

        throw new InvalidOperationException($"Adding entity map for type '{typeof(TEntity)}' failed. The type already exists. Current entity maps: " + string.Join(", ", FluentMapper.EntityMaps.Select((KeyValuePair<Type, IEntityMap> e) => e.Key.ToString())));
    }
}
