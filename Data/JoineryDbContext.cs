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

        // Configure GitRepository entity
        modelBuilder.Entity<GitRepository>(entity =>
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
        });

        // Configure GitQueryFile entity
        modelBuilder.Entity<GitQueryFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GitRepositoryId).IsRequired();
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DatabaseType).HasMaxLength(50);
            entity.Property(e => e.LastCommitAuthor).HasMaxLength(100);
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            // Relationship: GitQueryFile -> GitRepository
            entity.HasOne(e => e.GitRepository)
                  .WithMany(r => r.QueryFiles)
                  .HasForeignKey(e => e.GitRepositoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique combination of GitRepositoryId and FilePath
            entity.HasIndex(e => new { e.GitRepositoryId, e.FilePath }).IsUnique();
        });

        // Configure OrganizationAwsIamConfig entity
        modelBuilder.Entity<OrganizationAwsIamConfig>(entity =>
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
        });

        // Configure OrganizationEntraIdConfig entity
        modelBuilder.Entity<OrganizationEntraIdConfig>(entity =>
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
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RevokedByIp).HasMaxLength(100);
            entity.Property(e => e.ReasonRevoked).HasMaxLength(500);
            entity.Property(e => e.Version).IsRequired();

            // Relationship: RefreshToken -> User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index for faster token lookups
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsRevoked });
        });

        // Configure BlacklistedToken entity
        modelBuilder.Entity<BlacklistedToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.BlacklistedByIp).HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.TokenType).IsRequired().HasMaxLength(20);

            // Relationship: BlacklistedToken -> User (optional)
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Index for faster token hash lookups
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Configure ApiKey entity
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LastUsedFromIp).HasMaxLength(100);
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            entity.Property(e => e.RevokedByIp).HasMaxLength(100);
            entity.Property(e => e.Scopes).HasMaxLength(1000);
            entity.Property(e => e.UserId).IsRequired();

            // Relationship: ApiKey -> User
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.KeyPrefix);
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.LastUsedAt);
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
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