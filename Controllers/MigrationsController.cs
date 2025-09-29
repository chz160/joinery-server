using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JoineryServer.Services;
using JoineryServer.Models;

namespace JoineryServer.Controllers;

/// <summary>
/// Controller for database migration management
/// Provides endpoints for managing database schema migrations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all migration endpoints
public class MigrationsController : ControllerBase
{
    private readonly IMigrationService _migrationService;
    private readonly ILogger<MigrationsController> _logger;

    public MigrationsController(IMigrationService migrationService, ILogger<MigrationsController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Get current migration status
    /// </summary>
    /// <returns>Current migration status including applied and pending migrations</returns>
    [HttpGet("status")]
    public async Task<ActionResult<MigrationStatus>> GetStatus()
    {
        try
        {
            var status = await _migrationService.GetMigrationStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving migration status");
            return StatusCode(500, new { error = "Failed to retrieve migration status", message = ex.Message });
        }
    }

    /// <summary>
    /// Apply all pending migrations
    /// </summary>
    /// <param name="dryRun">If true, preview changes without applying them</param>
    /// <returns>Migration result</returns>
    [HttpPost("apply")]
    public async Task<ActionResult<MigrationResult>> ApplyMigrations([FromQuery] bool dryRun = false)
    {
        try
        {
            if (!dryRun && !IsProductionSafe())
            {
                return BadRequest(new { error = "Production migrations require additional safeguards" });
            }

            using var migrationLock = await _migrationService.TryAcquireLockAsync(TimeSpan.FromMinutes(5));
            if (migrationLock == null)
            {
                return Conflict(new { error = "Another migration is currently in progress" });
            }

            var result = await _migrationService.ApplyMigrationsAsync(dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations");
            return StatusCode(500, new { error = "Failed to apply migrations", message = ex.Message });
        }
    }

    /// <summary>
    /// Migrate to specific version
    /// </summary>
    /// <param name="targetMigration">Target migration name (null for latest)</param>
    /// <param name="dryRun">If true, preview changes without applying them</param>
    /// <returns>Migration result</returns>
    [HttpPost("migrate-to")]
    public async Task<ActionResult<MigrationResult>> MigrateTo(
        [FromQuery] string? targetMigration = null,
        [FromQuery] bool dryRun = false)
    {
        try
        {
            if (!dryRun && !IsProductionSafe())
            {
                return BadRequest(new { error = "Production migrations require additional safeguards" });
            }

            using var migrationLock = await _migrationService.TryAcquireLockAsync(TimeSpan.FromMinutes(5));
            if (migrationLock == null)
            {
                return Conflict(new { error = "Another migration is currently in progress" });
            }

            var result = await _migrationService.MigrateToAsync(targetMigration, dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating to target");
            return StatusCode(500, new { error = "Failed to migrate to target", message = ex.Message });
        }
    }

    /// <summary>
    /// Generate SQL script for migrations
    /// </summary>
    /// <param name="fromMigration">Starting migration (null for beginning)</param>
    /// <param name="toMigration">Ending migration (null for latest)</param>
    /// <param name="idempotent">Generate idempotent script</param>
    /// <returns>SQL script</returns>
    [HttpGet("script")]
    public async Task<ActionResult<object>> GenerateScript(
        [FromQuery] string? fromMigration = null,
        [FromQuery] string? toMigration = null,
        [FromQuery] bool idempotent = true)
    {
        try
        {
            var script = await _migrationService.GenerateScriptAsync(fromMigration, toMigration, idempotent);
            return Ok(new { script, fromMigration, toMigration, idempotent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating migration script");
            return StatusCode(500, new { error = "Failed to generate script", message = ex.Message });
        }
    }

    /// <summary>
    /// Validate migration integrity
    /// </summary>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    public async Task<ActionResult<MigrationValidationResult>> ValidateMigrations()
    {
        try
        {
            var result = await _migrationService.ValidateMigrationsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating migrations");
            return StatusCode(500, new { error = "Failed to validate migrations", message = ex.Message });
        }
    }

    /// <summary>
    /// Check database connectivity
    /// </summary>
    /// <returns>Connection status</returns>
    [HttpGet("connection-test")]
    public async Task<ActionResult<object>> TestConnection()
    {
        try
        {
            var canConnect = await _migrationService.CanConnectToDatabaseAsync();
            return Ok(new { canConnect, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return StatusCode(500, new { error = "Failed to test connection", message = ex.Message });
        }
    }

    /// <summary>
    /// Attempt to acquire migration lock
    /// </summary>
    /// <param name="timeoutMinutes">Lock timeout in minutes</param>
    /// <returns>Lock information</returns>
    [HttpPost("lock")]
    public async Task<ActionResult<object>> AcquireLock([FromQuery] int timeoutMinutes = 5)
    {
        try
        {
            var timeout = TimeSpan.FromMinutes(Math.Max(1, Math.Min(30, timeoutMinutes))); // 1-30 minutes
            using var migrationLock = await _migrationService.TryAcquireLockAsync(timeout);

            if (migrationLock != null)
            {
                return Ok(new
                {
                    acquired = true,
                    lockId = migrationLock.LockId,
                    acquiredAt = migrationLock.AcquiredAt,
                    expiresAt = migrationLock.ExpiresAt
                });
            }
            else
            {
                return Ok(new { acquired = false, reason = "Lock timeout" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring migration lock");
            return StatusCode(500, new { error = "Failed to acquire lock", message = ex.Message });
        }
    }

    /// <summary>
    /// Check production safety for migrations
    /// </summary>
    /// <returns>True if production migrations are allowed</returns>
    private bool IsProductionSafe()
    {
        // In development or when InMemory database is used, always allow
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true ||
               environment?.Equals("Testing", StringComparison.OrdinalIgnoreCase) == true;
    }
}