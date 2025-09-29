using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JoineryServer.Data;

/// <summary>
/// Design-time factory for creating DbContext instances for EF Core migrations.
/// This allows migrations to be created even when the application uses InMemory database.
/// </summary>
public class JoineryDbContextFactory : IDesignTimeDbContextFactory<JoineryDbContext>
{
    public JoineryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JoineryDbContext>();

        // Use PostgreSQL for migrations with a placeholder connection string
        // In production/development, actual connection strings will be provided via configuration
        optionsBuilder.UseNpgsql("Host=localhost;Database=joinery_migrations;Username=postgres;Password=password");

        return new JoineryDbContext(optionsBuilder.Options);
    }
}