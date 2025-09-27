using JoineryServer.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JoineryServer.Middleware;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        // Skip validation for non-authenticated endpoints
        if (!context.Request.Path.StartsWithSegments("/api") || 
            context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var token = ExtractTokenFromHeader(context.Request);
        
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Check if token is blacklisted
                if (await tokenService.IsTokenBlacklistedAsync(token, "access"))
                {
                    _logger.LogWarning("Blacklisted token attempted access to {Path}", context.Request.Path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }

                // Validate token structure and extract claims
                if (ValidateTokenStructure(token))
                {
                    // Token is valid, continue to next middleware
                    await _next(context);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed for {Path}", context.Request.Path);
            }
        }

        await _next(context);
    }

    private string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    private bool ValidateTokenStructure(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return false;
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            // Check if token has expired (additional check)
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired token attempted access");
                return false;
            }

            // Check if token has required claims
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("Token missing required user ID claim");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token structure validation failed");
            return false;
        }
    }
}