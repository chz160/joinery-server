using JoineryServer.Models;

namespace JoineryServer.Services;

/// <summary>
/// Service interface for managing database migrations at runtime
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Applies all pending migrations to the database
    /// </summary>
    /// <param name="dryRun">If true, returns operations without executing them</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result with applied migrations</returns>
    Task<MigrationResult> ApplyMigrationsAsync(bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates to a specific migration version
    /// </summary>
    /// <param name="targetMigration">Target migration name, or null for latest</param>
    /// <param name="dryRun">If true, returns operations without executing them</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration result with applied migrations</returns>
    Task<MigrationResult> MigrateToAsync(string? targetMigration = null, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current migration status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration status information</returns>
    Task<MigrationStatus> GetMigrationStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates SQL script for migrations
    /// </summary>
    /// <param name="fromMigration">Starting migration (null for beginning)</param>
    /// <param name="toMigration">Ending migration (null for latest)</param>
    /// <param name="idempotent">Generate idempotent script</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SQL script</returns>
    Task<string> GenerateScriptAsync(string? fromMigration = null, string? toMigration = null, bool idempotent = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates migration integrity using checksums
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a migration lock
    /// </summary>
    /// <param name="lockTimeout">Maximum time to wait for lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration lock or null if unable to acquire</returns>
    Task<IMigrationLock?> TryAcquireLockAsync(TimeSpan lockTimeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if database exists and is accessible
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if database is accessible</returns>
    Task<bool> CanConnectToDatabaseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a migration lock to prevent concurrent migrations
/// </summary>
public interface IMigrationLock : IDisposable
{
    /// <summary>
    /// Lock identifier
    /// </summary>
    string LockId { get; }

    /// <summary>
    /// When the lock was acquired
    /// </summary>
    DateTime AcquiredAt { get; }

    /// <summary>
    /// When the lock expires
    /// </summary>
    DateTime ExpiresAt { get; }
}