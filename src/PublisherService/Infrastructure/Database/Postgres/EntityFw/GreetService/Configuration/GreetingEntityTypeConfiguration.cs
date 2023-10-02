using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PublisherService.Core.GreetService.Entity;

namespace PublisherService.Infrastructure.Database.Postgres.EntityFw.GreetService.Configuration;

public class GreetingEntityTypeConfiguration : IEntityTypeConfiguration<GreetingEntity>
{
    public void Configure(EntityTypeBuilder<GreetingEntity> builder)
    {
        builder.ToTable("tbl_greeting");
    }
}