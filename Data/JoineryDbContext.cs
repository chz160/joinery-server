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
    public DbSet<OrganizationAwsIamConfig> OrganizationAwsIamConfigs { get; set; }
    public DbSet<OrganizationEntraIdConfig> OrganizationEntraIdConfigs { get; set; }
    public DbSet<GitRepository> GitRepositories { get; set; }
    public DbSet<GitQueryFile> GitQueryFiles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JoineryDbContext).Assembly);

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