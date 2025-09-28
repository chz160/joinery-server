using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.DeviceInfo).HasMaxLength(200);
        entity.Property(e => e.IpAddress).HasMaxLength(100);
        entity.Property(e => e.UserAgent).HasMaxLength(500);
        entity.Property(e => e.RevokedReason).HasMaxLength(500);
        entity.Property(e => e.RevokedByIp).HasMaxLength(100);
        entity.Property(e => e.LoginMethod).HasMaxLength(50);
        entity.Property(e => e.Location).HasMaxLength(100);
        entity.Property(e => e.SuspiciousReasons).HasMaxLength(1000);

        // Relationship: Session -> User
        entity.HasOne(e => e.User)
              .WithMany()
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        entity.HasIndex(e => e.SessionId).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.IsActive });
        entity.HasIndex(e => e.ExpiresAt);
        entity.HasIndex(e => e.LastActivityAt);
    }
}