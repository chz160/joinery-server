using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        entity.Property(e => e.FullName).HasMaxLength(200);
        entity.Property(e => e.AuthProvider).IsRequired().HasMaxLength(50);
        entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(100);
        entity.HasIndex(e => new { e.AuthProvider, e.ExternalId }).IsUnique();
    }
}