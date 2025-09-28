using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class OrganizationAwsIamConfigConfiguration : IEntityTypeConfiguration<OrganizationAwsIamConfig>
{
    public void Configure(EntityTypeBuilder<OrganizationAwsIamConfig> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.OrganizationId).IsRequired();
        entity.Property(e => e.AwsRegion).IsRequired().HasMaxLength(100);
        entity.Property(e => e.AccessKeyId).IsRequired().HasMaxLength(200);
        entity.Property(e => e.SecretAccessKey).IsRequired().HasMaxLength(500);
        entity.Property(e => e.RoleArn).HasMaxLength(200);
        entity.Property(e => e.ExternalId).HasMaxLength(100);

        // Relationship: OrganizationAwsIamConfig -> Organization
        entity.HasOne(e => e.Organization)
              .WithOne()
              .HasForeignKey<OrganizationAwsIamConfig>(e => e.OrganizationId)
              .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique configuration per organization
        entity.HasIndex(e => e.OrganizationId).IsUnique();
    }
}