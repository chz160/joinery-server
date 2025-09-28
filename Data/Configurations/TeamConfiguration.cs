using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JoineryServer.Models;

namespace JoineryServer.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Description).HasMaxLength(500);
        entity.Property(e => e.CreatedByUserId).IsRequired();
        entity.Property(e => e.OrganizationId).IsRequired();

        // Relationship: Team -> User (CreatedBy)
        entity.HasOne(e => e.CreatedByUser)
              .WithMany(u => u.CreatedTeams)
              .HasForeignKey(e => e.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Team -> Organization (required)
        entity.HasOne(e => e.Organization)
              .WithMany(o => o.Teams)
              .HasForeignKey(e => e.OrganizationId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}