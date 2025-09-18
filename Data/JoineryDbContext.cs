using Microsoft.EntityFrameworkCore;
using JoineryServer.Models;

namespace JoineryServer.Data;

public class JoineryDbContext : DbContext
{
    public JoineryDbContext(DbContextOptions<JoineryDbContext> options) : base(options)
    {
    }
    
    public DbSet<DatabaseQuery> DatabaseQueries { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure DatabaseQuery entity
        modelBuilder.Entity<DatabaseQuery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SqlQuery).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DatabaseType).HasMaxLength(50);
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        });
        
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.AuthProvider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.AuthProvider, e.ExternalId }).IsUnique();
        });
        
        // Configure Team entity
        modelBuilder.Entity<Team>(entity =>
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
        });
        
        // Configure TeamMember entity
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TeamId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            
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
        });
        
        // Configure Organization entity
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedByUserId).IsRequired();
            
            // Relationship: Organization -> User (CreatedBy)
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedOrganizations)
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure OrganizationMember entity
        modelBuilder.Entity<OrganizationMember>(entity =>
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
        });
        
        // Seed data for development
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatabaseQuery>().HasData(
            new DatabaseQuery
            {
                Id = 1,
                Name = "Sample User Query",
                SqlQuery = "SELECT * FROM users WHERE active = 1",
                Description = "Retrieves all active users",
                CreatedBy = "system",
                DatabaseType = "PostgreSQL",
                Tags = new List<string> { "users", "basic" }
            },
            new DatabaseQuery
            {
                Id = 2,
                Name = "User Count by Registration Date",
                SqlQuery = "SELECT DATE(created_at) as registration_date, COUNT(*) as user_count FROM users GROUP BY DATE(created_at) ORDER BY registration_date",
                Description = "Shows user registration counts by date",
                CreatedBy = "system",
                DatabaseType = "MySQL",
                Tags = new List<string> { "analytics", "users", "count" }
            }
        );
    }
}