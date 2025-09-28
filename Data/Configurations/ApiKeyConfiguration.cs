using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(20);
        entity.Property(e => e.LastUsedFromIp).HasMaxLength(100);
        entity.Property(e => e.RevokedReason).HasMaxLength(500);
        entity.Property(e => e.RevokedByIp).HasMaxLength(100);
        entity.Property(e => e.Scopes).HasMaxLength(1000);
        entity.Property(e => e.UserId).IsRequired();

        // Relationship: ApiKey -> User
        entity.HasOne(e => e.User)
              .WithMany()
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        entity.HasIndex(e => e.KeyHash).IsUnique();
        entity.HasIndex(e => e.KeyPrefix);
        entity.HasIndex(e => new { e.UserId, e.IsActive });
        entity.HasIndex(e => e.ExpiresAt);
        entity.HasIndex(e => e.LastUsedAt);
    }
}