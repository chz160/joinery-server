namespace JoineryServer.Models;

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Whether the migration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of migrations that were applied
    /// </summary>
    public List<string> AppliedMigrations { get; set; } = new();

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// SQL operations that were executed (for dry run mode)
    /// </summary>
    public List<string> SqlOperations { get; set; } = new();

    /// <summary>
    /// Duration of the migration operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether this was a dry run (no actual changes made)
    /// </summary>
    public bool IsDryRun { get; set; }
}

/// <summary>
/// Current migration status of the database
/// </summary>
public class MigrationStatus
{
    /// <summary>
    /// Whether the database exists
    /// </summary>
    public bool DatabaseExists { get; set; }

    /// <summary>
    /// Current migration version applied to database
    /// </summary>
    public string? CurrentMigration { get; set; }

    /// <summary>
    /// All available migrations
    /// </summary>
    public List<MigrationInfo> AvailableMigrations { get; set; } = new();

    /// <summary>
    /// Migrations applied to the database
    /// </summary>
    public List<MigrationInfo> AppliedMigrations { get; set; } = new();

    /// <summary>
    /// Migrations that are pending (not yet applied)
    /// </summary>
    public List<MigrationInfo> PendingMigrations { get; set; } = new();

    /// <summary>
    /// Database provider being used
    /// </summary>
    public string? DatabaseProvider { get; set; }

    /// <summary>
    /// Connection status
    /// </summary>
    public bool CanConnect { get; set; }

    /// <summary>
    /// When this status was retrieved
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Information about a specific migration
/// </summary>
public class MigrationInfo
{
    /// <summary>
    /// Migration identifier/name
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Migration display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product version of EF Core when migration was created
    /// </summary>
    public string? ProductVersion { get; set; }

    /// <summary>
    /// Whether this migration has been applied to the database
    /// </summary>
    public bool IsApplied { get; set; }

    /// <summary>
    /// When this migration was applied (if applicable)
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Checksum of the migration for validation
    /// </summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Result of migration validation
/// </summary>
public class MigrationValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> ValidationWarnings { get; set; } = new();

    /// <summary>
    /// Migrations with checksum mismatches
    /// </summary>
    public List<string> ChecksumMismatches { get; set; } = new();
}