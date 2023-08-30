using Microsoft.EntityFrameworkCore;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.GreetService.Entity;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.Context;

public class ApplicationDbContext : DbContext
{
    private readonly string _apiSchema = "public";
    private readonly string _connectionString;

    public ApplicationDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("ServiceDbOptions:ConnectionString").Value;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string read from configuration cannot be null.");
        }

        _connectionString = connectionString;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseNpgsql(_connectionString,
            b =>
            {
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                b.MigrationsHistoryTable("__EFMigrationsHistory", _apiSchema);
            });

        optionsBuilder.UseSnakeCaseNamingConvention();
    }
}
