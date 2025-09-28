using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.RevokedByIp).HasMaxLength(100);
        entity.Property(e => e.ReasonRevoked).HasMaxLength(500);
        entity.Property(e => e.Version).IsRequired();

        // Relationship: RefreshToken -> User
        entity.HasOne(e => e.User)
              .WithMany()
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Index for faster token lookups
        entity.HasIndex(e => e.Token).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.IsRevoked });
    }
}