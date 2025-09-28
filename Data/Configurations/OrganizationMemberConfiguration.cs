using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OrganizationId).IsRequired();
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.Role).IsRequired();

        // Relationship: OrganizationMember -> Organization
        entity.HasOne(e => e.Organization)
              .WithMany(o => o.OrganizationMembers)
              .HasForeignKey(e => e.OrganizationId)
              .OnDelete(DeleteBehavior.Cascade);

        // Relationship: OrganizationMember -> User
        entity.HasOne(e => e.User)
              .WithMany(u => u.OrganizationMemberships)
              .HasForeignKey(e => e.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique combination of OrganizationId and UserId
        entity.HasIndex(e => new { e.OrganizationId, e.UserId }).IsUnique();
    }
}