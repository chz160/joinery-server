using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class DatabaseQueryConfiguration : IEntityTypeConfiguration<DatabaseQuery>
{
    public void Configure(EntityTypeBuilder<DatabaseQuery> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.SqlQuery).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DatabaseType).HasMaxLength(50);
        entity.Property(e => e.Tags)
            .HasConversion(
                v => string.Join(',', v ?? new List<string>()),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
    }
}