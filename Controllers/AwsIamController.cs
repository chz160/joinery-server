using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JoineryServer.Data;
using JoineryServer.Models;
using JoineryServer.Services;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/aws-iam")]
[Authorize]
public class AwsIamController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly IAwsIamService _awsIamService;
    private readonly ILogger<AwsIamController> _logger;

    public AwsIamController(JoineryDbContext context, IAwsIamService awsIamService, ILogger<AwsIamController> logger)
    {
        _context = context;
        _awsIamService = awsIamService;
        _logger = logger;
    }

    /// <summary>
    /// Get AWS IAM configuration for an organization
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetAwsIamConfig(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can view AWS IAM configuration");
        }

        var config = await _context.OrganizationAwsIamConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return NotFound(new { message = "AWS IAM configuration not found" });
        }

        // Don't return sensitive data
        return Ok(new
        {
            config.Id,
            config.OrganizationId,
            config.AwsRegion,
            HasAccessKey = !string.IsNullOrEmpty(config.AccessKeyId),
            HasSecretKey = !string.IsNullOrEmpty(config.SecretAccessKey),
            config.RoleArn,
            config.ExternalId,
            config.IsActive,
            config.CreatedAt,
            config.UpdatedAt
        });
    }

    /// <summary>
    /// Configure AWS IAM for an organization
    /// </summary>
    [HttpPost("config")]
    public async Task<IActionResult> ConfigureAwsIam(int organizationId, [FromBody] AwsIamConfigRequest request)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can configure AWS IAM");
        }

        // Validate the organization exists
        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization == null || !organization.IsActive)
        {
            return NotFound(new { message = "Organization not found" });
        }

        // Validate AWS credentials
        var isValid = await _awsIamService.ValidateCredentialsAsync(
            request.AccessKeyId, 
            request.SecretAccessKey, 
            request.AwsRegion,
            request.RoleArn,
            request.ExternalId);

        if (!isValid)
        {
            return BadRequest(new { message = "Invalid AWS credentials or insufficient permissions" });
        }

        // Check if configuration already exists
        var existingConfig = await _context.OrganizationAwsIamConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId);

        if (existingConfig != null)
        {
            // Update existing configuration
            existingConfig.AwsRegion = request.AwsRegion;
            existingConfig.AccessKeyId = request.AccessKeyId;
            existingConfig.SecretAccessKey = request.SecretAccessKey;
            existingConfig.RoleArn = request.RoleArn;
            existingConfig.ExternalId = request.ExternalId;
            existingConfig.IsActive = true;
            existingConfig.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new configuration
            var newConfig = new OrganizationAwsIamConfig
            {
                OrganizationId = organizationId,
                AwsRegion = request.AwsRegion,
                AccessKeyId = request.AccessKeyId,
                SecretAccessKey = request.SecretAccessKey,
                RoleArn = request.RoleArn,
                ExternalId = request.ExternalId,
                IsActive = true
            };
            _context.OrganizationAwsIamConfigs.Add(newConfig);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("AWS IAM configuration updated for organization {OrganizationId} by user {UserId}", 
            organizationId, userId);

        return Ok(new { message = "AWS IAM configuration saved successfully" });
    }

    /// <summary>
    /// Remove AWS IAM configuration for an organization
    /// </summary>
    [HttpDelete("config")]
    public async Task<IActionResult> RemoveAwsIamConfig(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can remove AWS IAM configuration");
        }

        var config = await _context.OrganizationAwsIamConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return NotFound(new { message = "AWS IAM configuration not found" });
        }

        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("AWS IAM configuration removed for organization {OrganizationId} by user {UserId}", 
            organizationId, userId);

        return Ok(new { message = "AWS IAM configuration removed successfully" });
    }

    /// <summary>
    /// Import users from AWS IAM
    /// </summary>
    [HttpPost("import-users")]
    public async Task<IActionResult> ImportUsersFromAws(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can import users from AWS IAM");
        }

        var config = await _context.OrganizationAwsIamConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return BadRequest(new { message = "AWS IAM configuration not found" });
        }

        try
        {
            var awsUsers = await _awsIamService.GetIamUsersAsync(config);
            var importedUsers = new List<object>();

            foreach (var awsUser in awsUsers)
            {
                // Create or update user in our system
                var user = await GetOrCreateUser(awsUser.UserId, awsUser.Username, awsUser.Email, "AWS", awsUser.FullName);

                // Add user to organization if not already a member
                var existingMember = await _context.OrganizationMembers
                    .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == user.Id);

                if (existingMember == null)
                {
                    var orgMember = new OrganizationMember
                    {
                        OrganizationId = organizationId,
                        UserId = user.Id,
                        Role = OrganizationRole.Member,
                        JoinedAt = DateTime.UtcNow
                    };
                    _context.OrganizationMembers.Add(orgMember);
                }

                importedUsers.Add(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    IsNew = existingMember == null
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Imported {UserCount} users from AWS IAM for organization {OrganizationId}", 
                awsUsers.Count, organizationId);

            return Ok(new
            {
                message = $"Successfully imported {awsUsers.Count} users from AWS IAM",
                importedUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import users from AWS IAM for organization {OrganizationId}", organizationId);
            return StatusCode(500, new { message = "Failed to import users from AWS IAM" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }

    private async Task<bool> IsOrganizationAdmin(int organizationId, int userId)
    {
        return await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId && 
                          m.UserId == userId && 
                          m.Role == OrganizationRole.Administrator);
    }

    private async Task<User> GetOrCreateUser(string externalId, string username, string email, string authProvider, string? fullName = null)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.AuthProvider == authProvider);

        if (existingUser != null)
        {
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existingUser;
        }

        var newUser = new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            AuthProvider = authProvider,
            ExternalId = externalId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new user: {Username} via {AuthProvider}", username, authProvider);

        return newUser;
    }
}

public class AwsIamConfigRequest
{
    public string AwsRegion { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string? RoleArn { get; set; }
    public string? ExternalId { get; set; }
}