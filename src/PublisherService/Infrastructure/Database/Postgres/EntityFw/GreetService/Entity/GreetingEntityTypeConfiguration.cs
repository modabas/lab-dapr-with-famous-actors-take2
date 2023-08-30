using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.GreetService.Entity;

public class GreetingEntityTypeConfiguration : IEntityTypeConfiguration<GreetingEntity>
{
    public void Configure(EntityTypeBuilder<GreetingEntity> builder)
    {
        builder.ToTable("tbl_greeting");
    }
}