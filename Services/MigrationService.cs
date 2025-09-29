using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using JoineryServer.Data;
using JoineryServer.Models;

namespace JoineryServer.Services;

/// <summary>
/// Service for managing database migrations at runtime
/// </summary>
public class MigrationService : IMigrationService
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<MigrationService> _logger;
    private static readonly SemaphoreSlim _migrationSemaphore = new(1, 1);

    public MigrationService(JoineryDbContext context, ILogger<MigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MigrationResult> ApplyMigrationsAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        return await MigrateToAsync(null, dryRun, cancellationToken);
    }

    public async Task<MigrationResult> MigrateToAsync(string? targetMigration = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult { IsDryRun = dryRun };

        try
        {
            _logger.LogInformation("Starting migration to target: {TargetMigration} (DryRun: {DryRun})",
                targetMigration ?? "Latest", dryRun);

            // Check if database provider supports migrations (skip for InMemory)
            var databaseProvider = _context.Database.ProviderName;
            if (databaseProvider?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("InMemory database detected, skipping migration");
                result.Success = true;
                return result;
            }

            var migrator = _context.Database.GetService<IMigrator>();
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);

            if (dryRun)
            {
                // Generate SQL script for pending migrations
                var script = await GenerateScriptAsync(null, targetMigration, true, cancellationToken);
                result.SqlOperations.Add(script);
                result.AppliedMigrations.AddRange(pendingMigrations);
            }
            else
            {
                // Apply migrations
                await migrator.MigrateAsync(targetMigration, cancellationToken);
                result.AppliedMigrations.AddRange(pendingMigrations);

                _logger.LogInformation("Applied {Count} migrations", pendingMigrations.Count());
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<MigrationStatus> GetMigrationStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new MigrationStatus();

        try
        {
            status.CanConnect = await CanConnectToDatabaseAsync(cancellationToken);
            status.DatabaseProvider = _context.Database.ProviderName;

            if (!status.CanConnect)
            {
                return status;
            }

            // Check if database exists and has migrations table
            try
            {
                status.DatabaseExists = await _context.Database.CanConnectAsync(cancellationToken);

                if (status.DatabaseExists)
                {
                    // Get applied migrations
                    var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
                    status.AppliedMigrations = appliedMigrations.Select(m => new MigrationInfo
                    {
                        Id = m,
                        Name = GetMigrationDisplayName(m),
                        IsApplied = true
                    }).ToList();

                    status.CurrentMigration = appliedMigrations.LastOrDefault();

                    // Get all available migrations from assemblies
                    var migrator = _context.Database.GetService<IMigrator>();
                    var allMigrations = _context.Database.GetMigrations();

                    status.AvailableMigrations = allMigrations.Select(m => new MigrationInfo
                    {
                        Id = m,
                        Name = GetMigrationDisplayName(m),
                        IsApplied = appliedMigrations.Contains(m)
                    }).ToList();

                    // Calculate pending migrations
                    var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                    status.PendingMigrations = pendingMigrations.Select(m => new MigrationInfo
                    {
                        Id = m,
                        Name = GetMigrationDisplayName(m),
                        IsApplied = false
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve migration history");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving migration status");
        }

        return status;
    }

    public async Task<string> GenerateScriptAsync(string? fromMigration = null, string? toMigration = null, bool idempotent = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var migrator = _context.Database.GetService<IMigrator>();
            var script = migrator.GenerateScript(fromMigration, toMigration, idempotent ? MigrationsSqlGenerationOptions.Idempotent : MigrationsSqlGenerationOptions.Default);

            return script;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating migration script");
            throw;
        }
    }

    public async Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = new MigrationValidationResult();

        try
        {
            var status = await GetMigrationStatusAsync(cancellationToken);

            if (!status.CanConnect)
            {
                result.ValidationErrors.Add("Cannot connect to database");
                return result;
            }

            if (!status.DatabaseExists)
            {
                result.ValidationWarnings.Add("Database does not exist");
                result.IsValid = true; // Not an error for validation
                return result;
            }

            // Check for any orphaned migrations (applied but not in code)
            var availableMigrationIds = status.AvailableMigrations.Select(m => m.Id).ToHashSet();
            var orphanedMigrations = status.AppliedMigrations
                .Where(m => !availableMigrationIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToList();

            if (orphanedMigrations.Any())
            {
                result.ValidationWarnings.AddRange(
                    orphanedMigrations.Select(m => $"Migration '{m}' is applied but not found in code"));
            }

            result.IsValid = !result.ValidationErrors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration validation failed");
            result.ValidationErrors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task<IMigrationLock?> TryAcquireLockAsync(TimeSpan lockTimeout, CancellationToken cancellationToken = default)
    {
        var lockAcquired = await _migrationSemaphore.WaitAsync(lockTimeout, cancellationToken);

        if (lockAcquired)
        {
            return new MigrationLock(_migrationSemaphore);
        }

        return null;
    }

    public async Task<bool> CanConnectToDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // For InMemory database, always return true
            if (_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database connection test failed");
            return false;
        }
    }

    private static string GetMigrationDisplayName(string migrationId)
    {
        // Extract readable name from migration ID (e.g., "20250929011150_InitialCreate" -> "InitialCreate")
        var parts = migrationId.Split('_', 2);
        return parts.Length > 1 ? parts[1] : migrationId;
    }

    private static string ComputeChecksum(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Implementation of migration lock using SemaphoreSlim
/// </summary>
internal class MigrationLock : IMigrationLock
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public string LockId { get; }
    public DateTime AcquiredAt { get; }
    public DateTime ExpiresAt { get; }

    public MigrationLock(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
        LockId = Guid.NewGuid().ToString();
        AcquiredAt = DateTime.UtcNow;
        ExpiresAt = AcquiredAt.AddMinutes(30); // 30 minute timeout
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Release();
            _disposed = true;
        }
    }
}