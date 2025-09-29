# Database Migrations Guide

This document provides comprehensive guidance for managing database migrations in Joinery Server.

## Overview

Joinery Server uses Entity Framework Core migrations to manage database schema changes. The migration system provides:

- **Version Control**: All schema changes are tracked and versioned
- **Deployment Safety**: Idempotent scripts and production safeguards
- **Runtime Management**: API endpoints and CLI tools for migration operations
- **Environment Support**: Development, staging, and production workflows
- **Lock Mechanism**: Prevents concurrent migration execution
- **Validation**: Integrity checks and rollback capabilities

## Quick Start

### 1. Development Setup

The application works out-of-the-box with InMemory database for development:

```bash
# Start the application (uses InMemory database)
dotnet run

# Access Swagger UI
open http://localhost:5256/swagger
```

### 2. Creating Your First Migration

```bash
# Create a new migration
./migrate.sh create AddUserPreferences

# Check migration status
./migrate.sh status

# Apply migrations (development)
./migrate.sh apply
```

### 3. Production Deployment

```bash
# Generate deployment script
./migrate.sh script

# Apply to production (requires confirmation)
ASPNETCORE_ENVIRONMENT=Production ./migrate.sh apply
```

## Architecture

### Components

1. **JoineryDbContext**: Main database context with entity configurations
2. **JoineryDbContextFactory**: Design-time factory for migration tooling
3. **IMigrationService**: Runtime migration management service
4. **MigrationsController**: REST API for migration operations
5. **migrate.sh**: CLI management script

### Database Providers

- **Development**: InMemory database (default)
- **Production**: PostgreSQL (configurable)
- **Testing**: InMemory or SQL Server (configurable)

## API Reference

### REST Endpoints

All endpoints require authentication and are prefixed with `/api/migrations/`:

#### GET /status
Returns current migration status including applied and pending migrations.

```json
{
  "databaseExists": true,
  "currentMigration": "20250929011150_InitialCreate",
  "availableMigrations": [...],
  "appliedMigrations": [...],
  "pendingMigrations": [...],
  "databaseProvider": "Npgsql.EntityFrameworkCore.PostgreSQL",
  "canConnect": true,
  "retrievedAt": "2025-09-29T01:12:00Z"
}
```

#### POST /apply
Applies all pending migrations.

Query parameters:
- `dryRun`: boolean - Preview changes without applying them

```json
{
  "success": true,
  "appliedMigrations": ["20250929011150_InitialCreate"],
  "sqlOperations": ["CREATE TABLE ..."],
  "duration": "00:00:02.123",
  "isDryRun": false
}
```

#### POST /migrate-to
Migrates to a specific version.

Query parameters:
- `targetMigration`: string - Target migration name (null for latest)
- `dryRun`: boolean - Preview changes without applying them

#### GET /script
Generates SQL script for migrations.

Query parameters:
- `fromMigration`: string - Starting migration (null for beginning)
- `toMigration`: string - Ending migration (null for latest)
- `idempotent`: boolean - Generate idempotent script (default: true)

#### GET /validate
Validates migration integrity and consistency.

```json
{
  "isValid": true,
  "validationErrors": [],
  "validationWarnings": [],
  "checksumMismatches": []
}
```

#### GET /connection-test
Tests database connectivity.

```json
{
  "canConnect": true,
  "timestamp": "2025-09-29T01:12:00Z"
}
```

### CLI Commands

The `migrate.sh` script provides comprehensive CLI management:

#### Basic Commands

```bash
# Show migration status
./migrate.sh status

# Create new migration
./migrate.sh create AddNewFeature

# Apply all pending migrations
./migrate.sh apply

# Apply to specific migration
./migrate.sh apply-to 20250929011150_InitialCreate

# List all migrations
./migrate.sh list
```

#### Advanced Commands

```bash
# Generate SQL script
./migrate.sh script

# Generate script for specific range
./migrate.sh script-range 20250929011150_InitialCreate 20250929011200_AddNewFeature

# Validate migrations
./migrate.sh validate

# Test database connection
./migrate.sh connection-test

# Preview changes (dry run)
./migrate.sh dry-run

# Rollback to specific migration
./migrate.sh rollback 20250929011150_InitialCreate --force
```

#### Development Commands

```bash
# Reset all migrations (development only)
./migrate.sh reset --force

# Show help
./migrate.sh help
```

#### Command Options

- `--dry-run`: Preview operations without executing them
- `--force`: Skip confirmation prompts (use with caution)
- `--verbose`: Show detailed output
- `--provider <name>`: Specify database provider
- `--connection <string>`: Override connection string

## Configuration

### Connection Strings

Configure database providers in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=joinery;Username=postgres;Password=password"
  },
  "DatabaseProvider": "PostgreSQL"
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to Development, Staging, or Production
- `DATABASE_PROVIDER`: Override database provider
- `CONNECTION_STRING`: Override connection string

### Production Configuration

Update `Program.cs` to use real database provider:

```csharp
// Replace InMemory database with PostgreSQL
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

## Development Workflow

### 1. Making Schema Changes

1. Modify entity models or configurations
2. Create migration: `./migrate.sh create DescriptiveName`
3. Review generated migration files
4. Test migration: `./migrate.sh apply --dry-run`
5. Apply migration: `./migrate.sh apply`

### 2. Code Review Process

1. Include migration files in pull requests
2. Review Up() and Down() methods for correctness
3. Check for potential data loss operations
4. Validate migration naming conventions

### 3. Testing Migrations

```bash
# Test migration creation
./migrate.sh create TestMigration

# Test dry run
./migrate.sh apply --dry-run

# Test rollback (development only)
./migrate.sh rollback PreviousMigration --force

# Clean up test migration
./migrate.sh reset --force
```

## Production Deployment

### 1. Pre-Deployment Checklist

- [ ] Database server provisioned and accessible
- [ ] Database user created with appropriate permissions
- [ ] Connection string tested and secured
- [ ] Backup strategy implemented
- [ ] Monitoring and logging configured

### 2. Schema Deployment

```bash
# Generate deployment script
./migrate.sh script > deployment-$(date +%Y%m%d).sql

# Review generated SQL for safety
less deployment-$(date +%Y%m%d).sql

# Test in staging environment
ASPNETCORE_ENVIRONMENT=Staging ./migrate.sh apply

# Deploy to production (requires confirmation)
ASPNETCORE_ENVIRONMENT=Production ./migrate.sh apply
```

### 3. Rollback Procedures

```bash
# Check current status
./migrate.sh status

# Rollback to specific migration (requires --force)
./migrate.sh rollback TargetMigration --force

# Generate rollback script for manual execution
./migrate.sh script-range CurrentMigration TargetMigration
```

## Production Safeguards

### Environment Checks

- Production operations require explicit environment confirmation
- Reset command is disabled in production
- Rollback operations require `--force` flag

### Lock Mechanism

- Prevents concurrent migration execution
- Automatic timeout after 30 minutes
- Lock status visible in API responses

### Validation

- Migration integrity checks with checksums
- Orphaned migration detection
- Model snapshot validation

## Troubleshooting

### Common Issues

#### Migration Creation Fails

```bash
# Check EF Core tools installation
dotnet ef --version

# Install if missing
dotnet tool install --global dotnet-ef

# Verify project builds
dotnet build
```

#### Connection Issues

```bash
# Test connection
./migrate.sh connection-test

# Check connection string
dotnet ef dbcontext info --context JoineryDbContext

# Verify database provider packages
dotnet list package | grep EntityFrameworkCore
```

#### Migration Conflicts

```bash
# List applied migrations
./migrate.sh list

# Reset migrations (development only)
./migrate.sh reset --force

# Manual cleanup
rm -rf Migrations/
./migrate.sh create InitialCreate
```

### Performance Issues

- Large migrations should be split into smaller batches
- Index creation can be done offline for large tables
- Consider maintenance windows for schema changes

### Data Migration

For complex data migrations, create separate migration classes:

```csharp
public partial class MigrateUserData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Schema changes first
        migrationBuilder.AddColumn<string>("NewColumn", "Users");
        
        // Data migration
        migrationBuilder.Sql(@"
            UPDATE Users 
            SET NewColumn = CASE 
                WHEN OldColumn IS NULL THEN 'DefaultValue'
                ELSE OldColumn 
            END
        ");
        
        // Cleanup old column
        migrationBuilder.DropColumn("OldColumn", "Users");
    }
}
```

## Best Practices

### Migration Naming

- Use descriptive names: `AddUserPreferences`, `UpdateOrderStatus`
- Include ticket numbers: `JIRA-123_AddUserPreferences`
- Avoid generic names: `UpdateDatabase`, `FixBug`

### Migration Content

- Keep migrations focused on single features
- Include both schema and data changes in same migration when related
- Add appropriate indexes for new columns
- Use transactions for complex operations

### Testing

- Always test migrations in staging environment
- Use dry-run mode to preview changes
- Create test data to validate migration behavior
- Test rollback procedures

### Documentation

- Comment complex migration logic
- Update schema documentation after migrations
- Document breaking changes in release notes
- Maintain migration changelog

## Integration with CI/CD

### Build Pipeline

```yaml
# Example GitHub Actions workflow
- name: Install EF Core Tools
  run: dotnet tool install --global dotnet-ef

- name: Generate Migration Script
  run: |
    cd src/JoineryServer
    ./migrate.sh script > ../../artifacts/migration-$(date +%Y%m%d).sql

- name: Validate Migrations
  run: |
    cd src/JoineryServer
    ./migrate.sh validate
```

### Deployment Pipeline

```yaml
- name: Apply Database Migrations
  run: |
    cd src/JoineryServer
    ASPNETCORE_ENVIRONMENT=Production ./migrate.sh apply
  env:
    CONNECTION_STRING: ${{ secrets.DATABASE_CONNECTION_STRING }}
```

## Security Considerations

### Access Control

- Migration endpoints require authentication
- Production deployments should use service accounts
- Limit database permissions to minimum required

### Connection Strings

- Store connection strings in secure configuration
- Use environment variables for sensitive data
- Rotate database credentials regularly

### Audit Trail

- All migration operations are logged
- Track who applied migrations and when
- Monitor for unauthorized schema changes

## Monitoring and Alerting

### Metrics to Track

- Migration execution time
- Failed migration attempts
- Schema drift detection
- Database size growth

### Alerts

- Failed migration deployments
- Long-running migrations
- Connection failures
- Schema validation errors

For additional support, see [DATABASE.md](DATABASE.md) for detailed schema information.