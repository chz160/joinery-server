using JoineryServer.Models;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace JoineryServer.Services;

public interface IRateLimitingService
{
    bool IsAllowed(string clientId, string endpoint);
    Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, UserAuthLevel authLevel);
    Task<RateLimitResult> CheckRateLimitAsync(HttpContext context, string endpointCategory = "");
    RateLimitSettings GetRateLimitSettings(string endpointCategory, UserAuthLevel authLevel);
    UserAuthLevel GetUserAuthLevel(HttpContext context);
}

public class RateLimitingService : IRateLimitingService
{
    private readonly IRateLimitStore _store;
    private readonly RateLimitConfig _config;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(
        IRateLimitStore store,
        IOptions<RateLimitConfig> config,
        ILogger<RateLimitingService> logger)
    {
        _store = store;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public bool IsAllowed(string clientId, string endpoint)
    {
        var result = CheckRateLimitAsync(clientId, endpoint, UserAuthLevel.Anonymous).GetAwaiter().GetResult();
        return result.IsAllowed;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, UserAuthLevel authLevel)
    {
        var endpointCategory = DetermineEndpointCategory(endpoint);
        var settings = GetRateLimitSettings(endpointCategory, authLevel);

        return await _store.CheckRateLimitAsync(clientId, endpoint, authLevel, settings);
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(HttpContext context, string endpointCategory = "")
    {
        var clientId = GetClientId(context);
        var endpoint = context.Request.Path.Value ?? "";
        var authLevel = GetUserAuthLevel(context);

        if (string.IsNullOrEmpty(endpointCategory))
        {
            endpointCategory = DetermineEndpointCategory(endpoint);
        }

        var settings = GetRateLimitSettings(endpointCategory, authLevel);

        return await _store.CheckRateLimitAsync(clientId, endpoint, authLevel, settings);
    }

    public RateLimitSettings GetRateLimitSettings(string endpointCategory, UserAuthLevel authLevel)
    {
        // Check for endpoint-specific settings first
        if (_config.Endpoints.TryGetValue(endpointCategory, out var endpointTiers))
        {
            return GetSettingsForAuthLevel(endpointTiers, authLevel);
        }

        // Fall back to global settings
        return GetSettingsForAuthLevel(_config.Global, authLevel);
    }

    public UserAuthLevel GetUserAuthLevel(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return UserAuthLevel.Anonymous;
        }

        // Check if user is admin (you can customize this logic based on your admin identification)
        var userRoles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (userRoles.Contains("Admin") || userRoles.Contains("admin"))
        {
            return UserAuthLevel.Admin;
        }

        return UserAuthLevel.Authenticated;
    }

    private RateLimitSettings GetSettingsForAuthLevel(RateLimitTiers tiers, UserAuthLevel authLevel)
    {
        return authLevel switch
        {
            UserAuthLevel.Admin => tiers.Admin,
            UserAuthLevel.Authenticated => tiers.Authenticated,
            UserAuthLevel.Anonymous => tiers.Anonymous,
            _ => tiers.Anonymous
        };
    }

    private string GetClientId(HttpContext context)
    {
        // For authenticated users, use user ID if available
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var userId = context.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }
        }

        // For anonymous users, use IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            return $"ip:{ipAddress}";
        }

        return "unknown";
    }

    private string DetermineEndpointCategory(string endpoint)
    {
        if (endpoint.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            return "Auth";
        }

        if (endpoint.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return "Health";
        }

        // Add more endpoint categories as needed
        return "Default";
    }
}