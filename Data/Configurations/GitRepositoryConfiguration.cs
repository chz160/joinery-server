using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class GitRepositoryConfiguration : IEntityTypeConfiguration<GitRepository>
{
    public void Configure(EntityTypeBuilder<GitRepository> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.RepositoryUrl).IsRequired().HasMaxLength(500);
        entity.Property(e => e.Branch).HasMaxLength(100);
        entity.Property(e => e.AccessToken).HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.CreatedByUserId).IsRequired();

        // Relationship: GitRepository -> User (CreatedBy)
        entity.HasOne(e => e.CreatedByUser)
              .WithMany()
              .HasForeignKey(e => e.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // Relationship: GitRepository -> Organization (optional)
        entity.HasOne(e => e.Organization)
              .WithMany()
              .HasForeignKey(e => e.OrganizationId)
              .OnDelete(DeleteBehavior.Cascade);

        // Relationship: GitRepository -> Team (optional)
        entity.HasOne(e => e.Team)
              .WithMany()
              .HasForeignKey(e => e.TeamId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}