using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JoineryServer.Models;
using JoineryServer.Services;
using System.Security.Claims;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new API key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var scopes = request.Scopes ?? new[] { "read" };

            var (apiKey, rawKey) = await _apiKeyService.GenerateApiKeyAsync(
                userId,
                request.Name,
                request.Description,
                request.ExpiresAt,
                scopes
            );

            var response = new CreateApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Description = apiKey.Description,
                KeyPrefix = apiKey.KeyPrefix,
                Key = rawKey, // Only shown once!
                Scopes = apiKey.GetScopes(),
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt
            };

            _logger.LogInformation("API key created for user {UserId} with name '{Name}'", userId, request.Name);
            return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all API keys for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApiKeys()
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId);

            var response = apiKeys.Select(k => new ApiKeyListItem
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                KeyPrefix = k.KeyPrefix,
                Scopes = k.GetScopes(),
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                IsActive = k.IsActive,
                IsRevoked = k.IsRevoked,
                IsExpired = k.IsExpired
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys for user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific API key by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKey(int id)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null)
            {
                return NotFound("API key not found");
            }

            // Ensure user owns this API key
            if (apiKey.UserId != userId)
            {
                return Forbid("Access denied");
            }

            var response = new ApiKeyDetails
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Description = apiKey.Description,
                KeyPrefix = apiKey.KeyPrefix,
                Scopes = apiKey.GetScopes(),
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                LastUsedFromIp = apiKey.LastUsedFromIp,
                IsActive = apiKey.IsActive,
                IsRevoked = apiKey.IsRevoked,
                IsExpired = apiKey.IsExpired,
                RevokedAt = apiKey.RevokedAt,
                RevokedReason = apiKey.RevokedReason
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(int id, [FromBody] RevokeApiKeyRequest? request = null)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user authentication");
            }

            var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null)
            {
                return NotFound("API key not found");
            }

            // Ensure user owns this API key
            if (apiKey.UserId != userId)
            {
                return Forbid("Access denied");
            }

            var reason = request?.Reason ?? "Revoked by user";
            var ipAddress = GetClientIpAddress();

            var success = await _apiKeyService.RevokeApiKeyAsync(id, reason, ipAddress);
            if (!success)
            {
                return BadRequest("Failed to revoke API key");
            }

            _logger.LogInformation("API key {KeyPrefix}... revoked by user {UserId}", apiKey.KeyPrefix, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string[]? Scopes { get; set; }
}

public class CreateApiKeyResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // Only included in create response
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired { get; set; }
}

public class ApiKeyDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KeyPrefix { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedFromIp { get; set; }
    public bool IsActive { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
}

public class RevokeApiKeyRequest
{
    public string Reason { get; set; } = string.Empty;
}