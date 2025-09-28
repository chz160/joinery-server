using JoineryServer.Services;
using System.Security.Claims;

namespace JoineryServer.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;

    public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        // Skip session validation for non-authenticated endpoints
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await _next(context);
            return;
        }

        var sessionId = ExtractSessionIdFromRequest(context.Request);

        if (!string.IsNullOrEmpty(sessionId))
        {
            try
            {
                // Validate session
                if (await sessionService.IsSessionValidAsync(sessionId))
                {
                    // Update session activity
                    await sessionService.UpdateSessionActivityAsync(sessionId);

                    // Detect anomalous activity
                    var ipAddress = GetClientIpAddress(context);
                    var userAgent = context.Request.Headers["User-Agent"].ToString();
                    await sessionService.DetectAnomalousActivityAsync(sessionId, ipAddress, userAgent);

                    // Add session ID to context for use in controllers
                    context.Items["SessionId"] = sessionId;

                    await _next(context);
                    return;
                }
                else
                {
                    _logger.LogWarning("Invalid session {SessionId} attempted access to {Path}",
                        sessionId, context.Request.Path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Session has expired or is invalid");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session validation failed for {SessionId} on {Path}",
                    sessionId, context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Session validation failed");
                return;
            }
        }

        // No session ID found - continue to JWT validation
        await _next(context);
    }

    private string? ExtractSessionIdFromRequest(HttpRequest request)
    {
        // Try to get session ID from custom header first
        var sessionId = request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionId))
            return sessionId;

        // Try to get from cookie
        if (request.Cookies.TryGetValue("SessionId", out var cookieSessionId))
            return cookieSessionId;

        // Try to extract from JWT claims if user is authenticated
        var user = request.HttpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst("session_id")?.Value;
        }

        return null;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}