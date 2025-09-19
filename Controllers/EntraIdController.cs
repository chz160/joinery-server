using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JoineryServer.Data;
using JoineryServer.Models;
using JoineryServer.Services;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/organizations/{organizationId}/entra-id")]
[Authorize]
public class EntraIdController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly IEntraIdService _entraIdService;
    private readonly ILogger<EntraIdController> _logger;

    public EntraIdController(JoineryDbContext context, IEntraIdService entraIdService, ILogger<EntraIdController> logger)
    {
        _context = context;
        _entraIdService = entraIdService;
        _logger = logger;
    }

    /// <summary>
    /// Get Entra ID configuration for an organization
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetEntraIdConfig(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can view Entra ID configuration");
        }

        var config = await _context.OrganizationEntraIdConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return NotFound(new { message = "Entra ID configuration not found" });
        }

        // Don't return sensitive data
        return Ok(new
        {
            config.Id,
            config.OrganizationId,
            config.TenantId,
            config.ClientId,
            HasClientSecret = !string.IsNullOrEmpty(config.ClientSecret),
            config.Domain,
            config.IsActive,
            config.CreatedAt,
            config.UpdatedAt
        });
    }

    /// <summary>
    /// Configure Entra ID for an organization
    /// </summary>
    [HttpPost("config")]
    public async Task<IActionResult> ConfigureEntraId(int organizationId, [FromBody] EntraIdConfigRequest request)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can configure Entra ID");
        }

        // Validate the organization exists
        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization == null || !organization.IsActive)
        {
            return NotFound(new { message = "Organization not found" });
        }

        // Check for existing authentication methods
        var hasAwsConfig = await _context.OrganizationAwsIamConfigs
            .AnyAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (hasAwsConfig)
        {
            return BadRequest(new { message = "Organization already has AWS IAM authentication configured. Remove it before configuring Entra ID." });
        }

        // Validate Entra ID credentials
        var isValid = await _entraIdService.ValidateCredentialsAsync(
            request.TenantId,
            request.ClientId,
            request.ClientSecret,
            request.Domain);

        if (!isValid)
        {
            return BadRequest(new { message = "Invalid Entra ID credentials or insufficient permissions" });
        }

        // Check if configuration already exists
        var existingConfig = await _context.OrganizationEntraIdConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId);

        if (existingConfig != null)
        {
            // Update existing configuration
            existingConfig.TenantId = request.TenantId;
            existingConfig.ClientId = request.ClientId;
            existingConfig.ClientSecret = request.ClientSecret;
            existingConfig.Domain = request.Domain;
            existingConfig.IsActive = true;
            existingConfig.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new configuration
            var newConfig = new OrganizationEntraIdConfig
            {
                OrganizationId = organizationId,
                TenantId = request.TenantId,
                ClientId = request.ClientId,
                ClientSecret = request.ClientSecret,
                Domain = request.Domain,
                IsActive = true
            };
            _context.OrganizationEntraIdConfigs.Add(newConfig);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Entra ID configuration updated for organization {OrganizationId} by user {UserId}",
            organizationId, userId);

        return Ok(new { message = "Entra ID configuration saved successfully" });
    }

    /// <summary>
    /// Remove Entra ID configuration for an organization
    /// </summary>
    [HttpDelete("config")]
    public async Task<IActionResult> RemoveEntraIdConfig(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can remove Entra ID configuration");
        }

        var config = await _context.OrganizationEntraIdConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return NotFound(new { message = "Entra ID configuration not found" });
        }

        config.IsActive = false;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Entra ID configuration removed for organization {OrganizationId} by user {UserId}",
            organizationId, userId);

        return Ok(new { message = "Entra ID configuration removed successfully" });
    }

    /// <summary>
    /// Import users from Entra ID
    /// </summary>
    [HttpPost("import-users")]
    public async Task<IActionResult> ImportUsersFromEntraId(int organizationId)
    {
        var userId = GetCurrentUserId();
        if (!await IsOrganizationAdmin(organizationId, userId))
        {
            return Forbid("Only organization administrators can import users from Entra ID");
        }

        var config = await _context.OrganizationEntraIdConfigs
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.IsActive);

        if (config == null)
        {
            return BadRequest(new { message = "Entra ID configuration not found" });
        }

        try
        {
            var entraIdUsers = await _entraIdService.GetEntraIdUsersAsync(config);
            var importedUsers = new List<object>();

            foreach (var entraIdUser in entraIdUsers)
            {
                // Create or update user in our system
                var fullName = $"{entraIdUser.GivenName} {entraIdUser.Surname}".Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    fullName = entraIdUser.DisplayName;
                }

                var user = await GetOrCreateUser(entraIdUser.UserId, entraIdUser.UserPrincipalName, entraIdUser.Email, "Microsoft", fullName);

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

            _logger.LogInformation("Imported {UserCount} users from Entra ID for organization {OrganizationId}",
                entraIdUsers.Count, organizationId);

            return Ok(new
            {
                message = $"Successfully imported {entraIdUsers.Count} users from Entra ID",
                importedUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import users from Entra ID for organization {OrganizationId}", organizationId);
            return StatusCode(500, new { message = "Failed to import users from Entra ID" });
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

public class EntraIdConfigRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Domain { get; set; }
}