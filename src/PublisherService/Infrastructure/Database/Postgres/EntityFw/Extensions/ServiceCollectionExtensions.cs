using Microsoft.EntityFrameworkCore;
using Npgsql;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.Context;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ServiceDbOptions:ConnectionString").Value;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string read from configuration cannot be null.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        //Configuration here...
        //
        var dataSource = dataSourceBuilder.Build();


        //Register Db context
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(dataSource, b =>
            {
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                b.MigrationsHistoryTable("__EFMigrationsHistory", ApplicationDbContext.ApiSchema);
            });
            options.UseSnakeCaseNamingConvention();
        });


        //...or Db context factory (this also registers db context)
        //services.AddDbContextFactory<ApplicationDbContext>(options =>
        //{
        //    options.UseNpgsql(dataSource, b =>
        //    {
        //        b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        //        b.MigrationsHistoryTable("__EFMigrationsHistory", ApplicationDbContext.ApiSchema);
        //    });
        //    options.UseSnakeCaseNamingConvention();
        //});

        return services;
    }
}
