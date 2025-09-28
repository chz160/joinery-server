using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TeamId).IsRequired();
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.Role).IsRequired();

        // Configure Permissions as nullable enum
        entity.Property(e => e.Permissions)
              .HasConversion<int?>()
              .IsRequired(false);

        // Relationship: TeamMember -> Team
        entity.HasOne(e => e.Team)
              .WithMany(t => t.TeamMembers)
              .HasForeignKey(e => e.TeamId)
              .OnDelete(DeleteBehavior.Cascade);

        // Relationship: TeamMember -> User
        entity.HasOne(e => e.User)
              .WithMany(u => u.TeamMemberships)
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique combination of TeamId and UserId
        entity.HasIndex(e => new { e.TeamId, e.UserId }).IsUnique();
    }
}