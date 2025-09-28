using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class OrganizationEntraIdConfigConfiguration : IEntityTypeConfiguration<OrganizationEntraIdConfig>
{
    public void Configure(EntityTypeBuilder<OrganizationEntraIdConfig> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OrganizationId).IsRequired();
        entity.Property(e => e.TenantId).IsRequired().HasMaxLength(200);
        entity.Property(e => e.ClientId).IsRequired().HasMaxLength(200);
        entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Domain).HasMaxLength(100);

        // Relationship: OrganizationEntraIdConfig -> Organization
        entity.HasOne(e => e.Organization)
              .WithOne()
              .HasForeignKey<OrganizationEntraIdConfig>(e => e.OrganizationId)
              .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique configuration per organization
        entity.HasIndex(e => e.OrganizationId).IsUnique();
    }
}