using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class BlacklistedTokenConfiguration : IEntityTypeConfiguration<BlacklistedToken>
{
    public void Configure(EntityTypeBuilder<BlacklistedToken> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.BlacklistedByIp).HasMaxLength(100);
        entity.Property(e => e.Reason).HasMaxLength(500);
        entity.Property(e => e.TokenType).IsRequired().HasMaxLength(20);

        // Relationship: BlacklistedToken -> User (optional)
        entity.HasOne(e => e.User)
              .WithMany()
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.SetNull);

        // Index for faster token hash lookups
        entity.HasIndex(e => e.TokenHash).IsUnique();
        entity.HasIndex(e => e.ExpiresAt);
    }
}