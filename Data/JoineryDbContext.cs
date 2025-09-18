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