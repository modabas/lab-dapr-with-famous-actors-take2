using Microsoft.EntityFrameworkCore;
using PublisherService.Core.GreetService.Entity;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.GreetService.Configuration;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.Context;

public class ApplicationDbContext : DbContext
{
    private static readonly string _apiSchema = "public";

    public static string ApiSchema => _apiSchema;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        :base(options)
    {
    }

    public virtual DbSet<GreetingEntity> Greetings => Set<GreetingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(_apiSchema);

        modelBuilder.ApplyConfiguration(new GreetingEntityTypeConfiguration());

        SeedDb(modelBuilder);
    }

    private void SeedDb(ModelBuilder modelBuilder) { }

}
