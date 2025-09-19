# Database Setup and Schema Deployment Guide

This guide provides comprehensive instructions for setting up and deploying the database schema for Joinery Server in both development and production environments.

## Overview

Joinery Server uses Entity Framework Core as the ORM and supports multiple database providers. The application includes a comprehensive data model with organizations, teams, users, database queries, and Git repository integration.

## Supported Database Providers

### 1. In-Memory Database (Development Only)
- **Provider**: `Microsoft.EntityFrameworkCore.InMemory`
- **Use Case**: Development, testing, and quick MVP setup
- **Advantages**: No external dependencies, fast setup, automatic reset
- **Limitations**: Data is lost on application restart, not suitable for production

### 2. SQL Server
- **Provider**: `Microsoft.EntityFrameworkCore.SqlServer`
- **Use Case**: Production environments, Windows-based deployments
- **Versions Supported**: SQL Server 2016+, Azure SQL Database
- **Features**: Full T-SQL support, high performance, enterprise features

### 3. PostgreSQL (Recommended for Production)
- **Provider**: `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Use Case**: Production environments, cross-platform deployments
- **Versions Supported**: PostgreSQL 10+
- **Features**: JSONB support, excellent performance, open-source

### 4. SQLite
- **Provider**: `Microsoft.EntityFrameworkCore.Sqlite`
- **Use Case**: Development, testing, lightweight deployments
- **Features**: File-based database, zero configuration, portable

### 5. MySQL/MariaDB
- **Provider**: `Pomelo.EntityFrameworkCore.MySql`
- **Use Case**: Production environments with existing MySQL infrastructure
- **Versions Supported**: MySQL 5.7+, MariaDB 10.2+

## Database Schema

The application includes the following main entities:

### Core Entities
- **Users**: Application users with authentication provider integration
- **Organizations**: Top-level organizational units with admin/member roles
- **Teams**: Team structures within organizations with granular permissions
- **DatabaseQueries**: Traditional database queries stored in the application

### Git Integration Entities
- **GitRepositories**: External Git repository configurations
- **GitQueryFiles**: SQL query files synchronized from Git repositories

### Authentication Integration Entities
- **OrganizationAwsIamConfigs**: AWS IAM integration settings per organization
- **OrganizationEntraIdConfigs**: Microsoft Entra ID integration settings per organization

### Association Entities
- **OrganizationMembers**: User membership in organizations with roles
- **TeamMembers**: User membership in teams with permissions

## Quick Setup Script

For a guided database setup experience, you can use the provided setup script:

```bash
# Make the script executable and run it
chmod +x setup-database.sh
./setup-database.sh
```

This interactive script will:
- Install the required database provider packages
- Update your configuration files
- Create and apply initial migrations
- Provide next steps and configuration guidance

For manual setup or advanced configuration, continue with the detailed instructions below.

## Development Database Setup

### Option 1: In-Memory Database (Default)

The application is pre-configured to use an in-memory database for development:

```csharp
// Program.cs - Already configured
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseInMemoryDatabase("JoineryDatabase"));
```

**No additional setup required** - the application will automatically create and seed the database on startup.

### Option 2: SQLite for Development

1. **Install SQLite provider**:
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

2. **Update Program.cs**:
```csharp
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

3. **Add connection string to appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=joinery_dev.db"
  }
}
```

4. **Create and apply migrations**:
```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to create database
dotnet ef database update
```

### Option 3: PostgreSQL for Development

1. **Install PostgreSQL provider**:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

2. **Setup local PostgreSQL**:
```bash
# Using Docker
docker run --name joinery-postgres \
  -e POSTGRES_USER=joineryuser \
  -e POSTGRES_PASSWORD=joinerypass \
  -e POSTGRES_DB=joinerydb \
  -p 5432:5432 \
  -d postgres:15
```

3. **Update Program.cs**:
```csharp
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

4. **Add connection string**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=joinerydb;Username=joineryuser;Password=joinerypass"
  }
}
```

## Production Database Setup

### PostgreSQL Production Setup

1. **Install NuGet package**:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

2. **Create production database**:
```sql
-- Connect as superuser
CREATE USER joineryuser WITH PASSWORD 'secure_password_here';
CREATE DATABASE joinerydb OWNER joineryuser;
GRANT ALL PRIVILEGES ON DATABASE joinerydb TO joineryuser;
```

3. **Update Program.cs for production**:
```csharp
if (app.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<JoineryDbContext>(options =>
        options.UseInMemoryDatabase("JoineryDatabase"));
}
else
{
    builder.Services.AddDbContext<JoineryDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}
```

4. **Production connection string** (in appsettings.Production.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-db-server;Database=joinerydb;Username=joineryuser;Password=your-secure-password;SSL Mode=Require"
  }
}
```

### SQL Server Production Setup

1. **Install NuGet package**:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

2. **Create production database**:
```sql
-- Connect as sa or admin user
CREATE DATABASE JoineryDB;
CREATE LOGIN joineryuser WITH PASSWORD = 'SecurePassword123!';
USE JoineryDB;
CREATE USER joineryuser FOR LOGIN joineryuser;
ALTER ROLE db_owner ADD MEMBER joineryuser;
```

3. **Update Program.cs**:
```csharp
builder.Services.AddDbContext<JoineryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

4. **Production connection string**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=JoineryDB;User Id=joineryuser;Password=SecurePassword123!;TrustServerCertificate=true"
  }
}
```

## Example Migration Commands

### Complete Migration Workflow

Here's a complete example workflow for setting up the database with Entity Framework migrations:

```bash
# 1. Install EF Core tools (one-time global installation)
dotnet tool install --global dotnet-ef

# 2. Add your preferred database provider (example with PostgreSQL)
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# 3. Update Program.cs to use your database provider (see examples above)

# 4. Create initial migration
dotnet ef migrations add InitialCreate --context JoineryDbContext

# 5. Review the generated migration files in Migrations/ folder

# 6. Apply migration to create database schema
dotnet ef database update --context JoineryDbContext

# 7. Verify migration was applied
dotnet ef migrations list --context JoineryDbContext
```

### Sample Migration Output

When you run `dotnet ef migrations add InitialCreate`, EF Core will generate migration files similar to:

```csharp
// Example: 20241215000000_InitialCreate.cs
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                AuthProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        // ... additional tables created here ...
        
        migrationBuilder.CreateIndex(
            name: "IX_Users_AuthProvider_ExternalId",
            table: "Users",
            columns: new[] { "AuthProvider", "ExternalId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");
        // ... drop other tables ...
    }
}
```

### Creating Migrations

1. **Install EF Core tools** (if not already installed):
```bash
dotnet tool install --global dotnet-ef
```

2. **Create initial migration**:
```bash
dotnet ef migrations add InitialCreate --context JoineryDbContext
```

3. **Create additional migrations for changes**:
```bash
dotnet ef migrations add AddNewFeature --context JoineryDbContext
```

### Applying Migrations

#### Development
```bash
# Apply all pending migrations
dotnet ef database update

# Apply specific migration
dotnet ef database update SpecificMigrationName
```

#### Production Deployment
```bash
# Generate SQL scripts for production deployment
dotnet ef migrations script --output schema.sql

# Or generate scripts for specific migration range
dotnet ef migrations script PreviousMigration LatestMigration --output update.sql
```

### Migration Management Commands

```bash
# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied to production)
dotnet ef migrations remove

# View migration status
dotnet ef database update --dry-run

# Generate script for all migrations
dotnet ef migrations script --idempotent --output complete-schema.sql
```

## Database Initialization and Seed Data

### Development Seed Data

The application automatically seeds development data when using the in-memory database. For persistent databases, you can trigger seeding by configuring Program.cs:

```csharp
// In Program.cs - modify the database setup section
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    
    // For in-memory database
    await context.Database.EnsureCreatedAsync();
    
    // For persistent databases, use migrations instead
    // await context.Database.MigrateAsync();
}
```

### Sample Data Included

The application seeds the following sample data for development:

#### Database Queries
1. **Sample User Query** (ID: 1)
   - SQL: `SELECT * FROM users WHERE active = 1`
   - Database Type: PostgreSQL
   - Tags: `users`, `basic`
   - Description: "Retrieves all active users"

2. **User Count by Registration Date** (ID: 2)
   - SQL: `SELECT DATE(created_at) as registration_date, COUNT(*) as user_count FROM users GROUP BY DATE(created_at) ORDER BY registration_date`
   - Database Type: MySQL
   - Tags: `analytics`, `users`, `count`
   - Description: "Shows user registration counts by date"

### Production Data Initialization

#### Option 1: Using Entity Framework Migrations

For production environments, use migrations to deploy schema and then run custom data initialization:

```csharp
// Create a custom data seeding service
public class DataSeeder
{
    private readonly JoineryDbContext _context;

    public DataSeeder(JoineryDbContext context)
    {
        _context = context;
    }

    public async Task SeedProductionDataAsync()
    {
        // Create system user if not exists
        if (!await _context.Users.AnyAsync(u => u.Username == "system"))
        {
            var systemUser = new User
            {
                Username = "system",
                Email = "system@yourcompany.com",
                FullName = "System User",
                AuthProvider = "System",
                ExternalId = "system-1",
                IsActive = true
            };
            _context.Users.Add(systemUser);
            await _context.SaveChangesAsync();
        }

        // Add other required production data
        await SeedOrganizationsAsync();
        await SeedQueriesAsync();
    }
}
```

#### Option 2: SQL Scripts for Production Data

Create SQL scripts for production data initialization:

```sql
-- production-data-init.sql
-- Create system user
INSERT INTO "Users" ("Username", "Email", "FullName", "AuthProvider", "ExternalId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'system', 'system@yourcompany.com', 'System User', 'System', 'system-1', true, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'system');

-- Create default organization
INSERT INTO "Organizations" ("Name", "Description", "CreatedByUserId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'Default Organization', 'System default organization', 
       (SELECT "Id" FROM "Users" WHERE "Username" = 'system'), 
       true, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Organizations" WHERE "Name" = 'Default Organization');

-- Add sample production queries
INSERT INTO "DatabaseQueries" ("Name", "SqlQuery", "Description", "CreatedBy", "DatabaseType", "Tags", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
  ('Health Check Query', 'SELECT 1 as health_status', 'Basic database connectivity test', 'system', 'PostgreSQL', 'health,monitoring', true, NOW(), NOW()),
  ('User Count', 'SELECT COUNT(*) as total_users FROM users', 'Get total user count', 'system', 'PostgreSQL', 'users,count', true, NOW(), NOW());
```

### Required Metadata Setup

#### Essential Configuration Data

For a fully functional production deployment, ensure the following metadata is configured:

1. **System User**: Required for system operations and Git repository management
2. **Default Organization**: Provides a base organization for initial users
3. **Administrator Roles**: Set up initial admin users with proper permissions
4. **Git Repository Configurations**: If using Git integration, configure initial repositories

#### Configuration Example

```csharp
// Production startup configuration in Program.cs
if (!app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    var dataSeeder = new DataSeeder(context);
    
    // Apply pending migrations
    await context.Database.MigrateAsync();
    
    // Initialize production data
    await dataSeeder.SeedProductionDataAsync();
}
```

### Custom Data Migration Scripts

For complex production deployments, create custom migration scripts:

#### PostgreSQL Production Init Script

```sql
-- postgresql-production-init.sql
-- Run after schema migrations are applied

BEGIN;

-- Create system user
INSERT INTO "Users" ("Username", "Email", "FullName", "AuthProvider", "ExternalId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'system', 'admin@yourcompany.com', 'System Administrator', 'System', 'sys-admin', true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'system');

-- Get system user ID for foreign key references
DO $$
DECLARE
    system_user_id INTEGER;
BEGIN
    SELECT "Id" INTO system_user_id FROM "Users" WHERE "Username" = 'system';
    
    -- Create default organization
    INSERT INTO "Organizations" ("Name", "Description", "CreatedByUserId", "IsActive", "CreatedAt", "UpdatedAt")
    SELECT 'Your Company', 'Main organization for company operations', system_user_id, true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
    WHERE NOT EXISTS (SELECT 1 FROM "Organizations" WHERE "Name" = 'Your Company');
    
END $$;

-- Create essential queries
INSERT INTO "DatabaseQueries" ("Name", "SqlQuery", "Description", "CreatedBy", "DatabaseType", "Tags", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
  ('System Health Check', 'SELECT NOW() as current_time, version() as db_version', 'System health and database version check', 'system', 'PostgreSQL', 'system,health', true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'),
  ('Active Users Count', 'SELECT COUNT(*) as active_users FROM "Users" WHERE "IsActive" = true', 'Count of active users in the system', 'system', 'PostgreSQL', 'users,analytics', true, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
ON CONFLICT DO NOTHING;

COMMIT;
```

#### SQL Server Production Init Script

```sql
-- sqlserver-production-init.sql
BEGIN TRANSACTION;

-- Create system user
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = 'system')
BEGIN
    INSERT INTO [Users] ([Username], [Email], [FullName], [AuthProvider], [ExternalId], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES ('system', 'admin@yourcompany.com', 'System Administrator', 'System', 'sys-admin', 1, GETUTCDATE(), GETUTCDATE());
END

-- Create default organization
DECLARE @SystemUserId INT = (SELECT [Id] FROM [Users] WHERE [Username] = 'system');

IF NOT EXISTS (SELECT 1 FROM [Organizations] WHERE [Name] = 'Your Company')
BEGIN
    INSERT INTO [Organizations] ([Name], [Description], [CreatedByUserId], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES ('Your Company', 'Main organization for company operations', @SystemUserId, 1, GETUTCDATE(), GETUTCDATE());
END

-- Create essential queries
IF NOT EXISTS (SELECT 1 FROM [DatabaseQueries] WHERE [Name] = 'System Health Check')
BEGIN
    INSERT INTO [DatabaseQueries] ([Name], [SqlQuery], [Description], [CreatedBy], [DatabaseType], [Tags], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES 
      ('System Health Check', 'SELECT GETUTCDATE() as current_time, @@VERSION as db_version', 'System health and database version check', 'system', 'SQL Server', 'system,health', 1, GETUTCDATE(), GETUTCDATE()),
      ('Active Users Count', 'SELECT COUNT(*) as active_users FROM [Users] WHERE [IsActive] = 1', 'Count of active users in the system', 'system', 'SQL Server', 'users,analytics', 1, GETUTCDATE(), GETUTCDATE());
END

COMMIT TRANSACTION;
```

### Deployment Commands

#### Apply Schema and Initialize Data

```bash
# 1. Apply all migrations to create schema
dotnet ef database update --connection "YourProductionConnectionString"

# 2. Run custom data initialization script
# For PostgreSQL:
psql -h your-server -U your-user -d your-database -f postgresql-production-init.sql

# For SQL Server:
sqlcmd -S your-server -d your-database -E -i sqlserver-production-init.sql

# 3. Verify initialization
dotnet run --environment Production
curl https://your-domain.com/api/health
```

### Data Validation Queries

After initialization, use these queries to verify your setup:

```sql
-- Verify system user exists
SELECT * FROM "Users" WHERE "Username" = 'system';

-- Check organization setup
SELECT o."Name", u."Username" as "CreatedBy" 
FROM "Organizations" o 
JOIN "Users" u ON o."CreatedByUserId" = u."Id";

-- Verify sample queries
SELECT "Name", "DatabaseType", "Tags" FROM "DatabaseQueries" WHERE "IsActive" = true;

-- Check database schema version
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
```

## Example SQL Scripts

### PostgreSQL Schema Creation Script

```sql
-- Complete PostgreSQL schema script
-- Generated with: dotnet ef migrations script --output postgresql-schema.sql

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Users table
CREATE TABLE "Users" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Username" character varying(100) NOT NULL,
    "Email" character varying(200) NOT NULL,
    "FullName" character varying(200),
    "AuthProvider" character varying(50) NOT NULL,
    "ExternalId" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Users_AuthProvider_ExternalId" ON "Users" ("AuthProvider", "ExternalId");

-- Organizations table
CREATE TABLE "Organizations" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "CreatedByUserId" integer NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    CONSTRAINT "PK_Organizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Organizations_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- Continue with other tables...
-- (This would be generated automatically by EF Core migrations)
```

### SQL Server Schema Creation Script

```sql
-- Complete SQL Server schema script
-- Generated with: dotnet ef migrations script --output sqlserver-schema.sql

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

-- Users table
IF OBJECT_ID(N'[Users]') IS NULL
BEGIN
    CREATE TABLE [Users] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [FullName] nvarchar(200) NULL,
        [AuthProvider] nvarchar(50) NOT NULL,
        [ExternalId] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_AuthProvider_ExternalId] ON [Users] ([AuthProvider], [ExternalId]);

-- Continue with other tables...
-- (This would be generated automatically by EF Core migrations)
```

## Connection String Examples

### Development Connection Strings

```json
{
  "ConnectionStrings": {
    // SQLite (file-based)
    "DefaultConnection": "Data Source=joinery.db",
    
    // PostgreSQL (local)
    "DefaultConnection": "Host=localhost;Database=joinerydb;Username=joineryuser;Password=devpassword",
    
    // SQL Server (local)
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=JoineryDB;Trusted_Connection=true;MultipleActiveResultSets=true",
    
    // MySQL (local)
    "DefaultConnection": "Server=localhost;Database=joinerydb;Uid=joineryuser;Pwd=devpassword;"
  }
}
```

### Production Connection Strings

```json
{
  "ConnectionStrings": {
    // PostgreSQL (production with SSL)
    "DefaultConnection": "Host=prod-db-server.example.com;Database=joinerydb;Username=joineryuser;Password=${DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=true",
    
    // SQL Server (production with encryption)
    "DefaultConnection": "Server=prod-sql-server.example.com;Database=JoineryDB;User Id=joineryuser;Password=${DB_PASSWORD};Encrypt=true;TrustServerCertificate=false",
    
    // Azure SQL Database
    "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=JoineryDB;User ID=joineryuser;Password=${DB_PASSWORD};Encrypt=true;Connection Timeout=30;",
    
    // AWS RDS PostgreSQL
    "DefaultConnection": "Host=yourdb.region.rds.amazonaws.com;Port=5432;Database=joinerydb;Username=joineryuser;Password=${DB_PASSWORD};SSL Mode=Require"
  }
}
```

## Production Deployment Checklist

### Pre-Deployment
- [ ] Database server provisioned and accessible
- [ ] Database user created with appropriate permissions
- [ ] Connection string tested and secured in configuration management
- [ ] Backup strategy implemented
- [ ] Monitoring and logging configured

### Schema Deployment
- [ ] Generate migration scripts: `dotnet ef migrations script --idempotent --output schema.sql`
- [ ] Review generated SQL scripts for production safety
- [ ] Test migration scripts in staging environment
- [ ] Apply schema changes during maintenance window
- [ ] Verify schema deployment with `dotnet ef migrations list`

### Data Initialization
- [ ] Apply initial seed data if required
- [ ] Create necessary system users and organizations
- [ ] Configure initial Git repositories if using Git integration
- [ ] Test application connectivity and basic operations

### Post-Deployment Verification
- [ ] Verify application starts successfully
- [ ] Test database connectivity: `/api/health` endpoint
- [ ] Validate user authentication flows
- [ ] Confirm query operations work correctly
- [ ] Monitor application logs for database-related errors

## Troubleshooting

### Common Issues

#### Migration Issues
```bash
# Reset migrations (development only)
dotnet ef database drop --force
dotnet ef migrations remove --force
dotnet ef migrations add InitialCreate
dotnet ef database update

# Fix migration history conflicts
UPDATE __EFMigrationsHistory SET ProductVersion = '8.0.20' WHERE MigrationId = 'YourMigrationId';
```

#### Connection Issues
```bash
# Test database connectivity
dotnet ef dbcontext info --connection "YourConnectionString"

# Verify connection string format
dotnet ef database update --connection "YourConnectionString" --dry-run
```

#### Permission Issues
```sql
-- PostgreSQL: Grant necessary permissions
GRANT CONNECT ON DATABASE joinerydb TO joineryuser;
GRANT USAGE ON SCHEMA public TO joineryuser;
GRANT CREATE ON SCHEMA public TO joineryuser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO joineryuser;

-- SQL Server: Add user to db_owner role
USE JoineryDB;
ALTER ROLE db_owner ADD MEMBER joineryuser;
```

### Performance Considerations

#### Indexing Strategy
```sql
-- Key indexes for performance (created automatically by EF Core)
-- Users table
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Username ON Users(Username);

-- Organizations and Teams
CREATE INDEX IX_OrganizationMembers_UserId ON OrganizationMembers(UserId);
CREATE INDEX IX_TeamMembers_UserId ON TeamMembers(UserId);

-- Git integration
CREATE INDEX IX_GitQueryFiles_GitRepositoryId ON GitQueryFiles(GitRepositoryId);
CREATE INDEX IX_GitQueryFiles_IsActive ON GitQueryFiles(IsActive) WHERE IsActive = 1;
```

#### Connection Pooling
```csharp
// Configure connection pooling in production
builder.Services.AddDbContext<JoineryDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(3);
    });
}, ServiceLifetime.Scoped);
```

## Security Considerations

### Connection String Security
- Never commit connection strings with passwords to source control
- Use environment variables or secure configuration providers
- Implement connection string encryption for sensitive environments
- Use managed identities when possible (Azure, AWS)

### Database Security
- Implement principle of least privilege for database users
- Use SSL/TLS encryption for database connections
- Regular security updates for database servers
- Monitor database access logs
- Implement database firewall rules

### Data Protection
- Encrypt sensitive data at rest
- Implement proper backup encryption
- Use parameterized queries (handled by EF Core)
- Regular security audits of data access patterns

---

For additional help with database setup, refer to the main [README.md](README.md) or create an issue in the repository.