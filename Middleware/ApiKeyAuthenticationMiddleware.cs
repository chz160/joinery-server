using JoineryServer.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JoineryServer.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService, ITokenService tokenService)
    {
        // Skip validation for non-authenticated endpoints
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var authResult = await TryAuthenticateAsync(context, apiKeyService, tokenService);

        if (authResult.IsAuthenticated)
        {
            // Set up the user principal for this request
            context.User = authResult.Principal!;
            await _next(context);
            return;
        }

        // Authentication failed
        if (authResult.AuthenticationAttempted)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(authResult.ErrorMessage ?? "Authentication failed");
            return;
        }

        // No authentication provided but endpoint requires it
        await _next(context);
    }

    private async Task<AuthenticationResult> TryAuthenticateAsync(HttpContext context, IApiKeyService apiKeyService, ITokenService tokenService)
    {
        // Check for API key first (header or query parameter)
        var apiKey = ExtractApiKey(context.Request);
        if (!string.IsNullOrEmpty(apiKey))
        {
            return await AuthenticateWithApiKeyAsync(apiKey, apiKeyService, context);
        }

        // Check for JWT token
        var jwtToken = ExtractJwtToken(context.Request);
        if (!string.IsNullOrEmpty(jwtToken))
        {
            return await AuthenticateWithJwtAsync(jwtToken, tokenService);
        }

        // No authentication provided
        return new AuthenticationResult { IsAuthenticated = false, AuthenticationAttempted = false };
    }

    private async Task<AuthenticationResult> AuthenticateWithApiKeyAsync(string apiKey, IApiKeyService apiKeyService, HttpContext context)
    {
        try
        {
            var keyData = await apiKeyService.ValidateApiKeyAsync(apiKey);
            if (keyData == null || !keyData.IsValid)
            {
                _logger.LogWarning("Invalid API key attempted access to {Path}", context.Request.Path);
                return new AuthenticationResult
                {
                    IsAuthenticated = false,
                    AuthenticationAttempted = true,
                    ErrorMessage = "Invalid or expired API key"
                };
            }

            // Update last used time
            var ipAddress = GetClientIpAddress(context);
            await apiKeyService.UpdateLastUsedAsync(keyData.Id, ipAddress);

            // Create claims principal for API key
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, keyData.UserId.ToString()),
                new Claim(ClaimTypes.Name, keyData.User.Username),
                new Claim(ClaimTypes.Email, keyData.User.Email),
                new Claim("auth_provider", "apikey"),
                new Claim("auth_type", "api_key"),
                new Claim("api_key_id", keyData.Id.ToString()),
                new Claim("api_key_name", keyData.Name)
            };

            // Add scope claims
            foreach (var scope in keyData.GetScopes())
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, "ApiKey");
            var principal = new ClaimsPrincipal(identity);

            _logger.LogDebug("Authenticated user {UserId} with API key '{KeyName}' for {Path}",
                keyData.UserId, keyData.Name, context.Request.Path);

            return new AuthenticationResult
            {
                IsAuthenticated = true,
                AuthenticationAttempted = true,
                Principal = principal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API key authentication");
            return new AuthenticationResult
            {
                IsAuthenticated = false,
                AuthenticationAttempted = true,
                ErrorMessage = "Authentication error"
            };
        }
    }

    private async Task<AuthenticationResult> AuthenticateWithJwtAsync(string jwtToken, ITokenService tokenService)
    {
        try
        {
            // Check if token is blacklisted
            if (await tokenService.IsTokenBlacklistedAsync(jwtToken, "access"))
            {
                _logger.LogWarning("Blacklisted JWT token attempted access");
                return new AuthenticationResult
                {
                    IsAuthenticated = false,
                    AuthenticationAttempted = true,
                    ErrorMessage = "Token has been revoked"
                };
            }

            // Validate token structure
            if (!ValidateJwtTokenStructure(jwtToken))
            {
                return new AuthenticationResult
                {
                    IsAuthenticated = false,
                    AuthenticationAttempted = true,
                    ErrorMessage = "Invalid token format"
                };
            }

            // JWT validation is handled by ASP.NET Core JWT middleware
            // We just need to verify it's not blacklisted
            return new AuthenticationResult { IsAuthenticated = false, AuthenticationAttempted = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JWT authentication");
            return new AuthenticationResult
            {
                IsAuthenticated = false,
                AuthenticationAttempted = true,
                ErrorMessage = "Authentication error"
            };
        }
    }

    private string? ExtractApiKey(HttpRequest request)
    {
        // Check Authorization header for API key
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("ApiKey "))
        {
            return authHeader.Substring("ApiKey ".Length).Trim();
        }

        // Check X-API-Key header
        var apiKeyHeader = request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKeyHeader))
        {
            return apiKeyHeader.Trim();
        }

        // Check query parameter
        var queryApiKey = request.Query["api_key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryApiKey))
        {
            return queryApiKey.Trim();
        }

        return null;
    }

    private string? ExtractJwtToken(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    private bool ValidateJwtTokenStructure(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return false;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Check if token has expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired JWT token attempted access");
                return false;
            }

            // Check if token has required claims
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("JWT token missing required user ID claim");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT token structure validation failed");
            return false;
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class AuthenticationResult
{
    public bool IsAuthenticated { get; set; }
    public bool AuthenticationAttempted { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public string? ErrorMessage { get; set; }
}