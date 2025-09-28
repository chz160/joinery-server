using JoineryServer.Models;
using JoineryServer.Services;
using Microsoft.Extensions.Options;

namespace JoineryServer.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitingService rateLimitingService)
    {
        // Skip rate limiting for health endpoints as they should always be available
        if (ShouldSkipRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var rateLimitResult = await rateLimitingService.CheckRateLimitAsync(context);

            // Add rate limit headers to response
            AddRateLimitHeaders(context.Response, rateLimitResult);

            if (!rateLimitResult.IsAllowed)
            {
                await HandleRateLimitExceeded(context, rateLimitResult);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rate limiting middleware failed, allowing request to proceed");
            await _next(context);
        }
    }

    private bool ShouldSkipRateLimit(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? "";

        // Skip health endpoints
        if (pathValue.StartsWith("/api/health"))
        {
            return true;
        }

        // Skip static files and swagger
        if (pathValue.StartsWith("/swagger") ||
            pathValue.StartsWith("/_framework") ||
            pathValue.StartsWith("/css") ||
            pathValue.StartsWith("/js") ||
            pathValue.StartsWith("/images"))
        {
            return true;
        }

        return false;
    }

    private void AddRateLimitHeaders(HttpResponse response, RateLimitResult result)
    {
        // Standard rate limit headers
        response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)result.ResetTime).ToUnixTimeSeconds().ToString();

        // Additional informational headers
        response.Headers["X-RateLimit-Policy"] = $"{result.AuthLevel}";

        if (!result.IsAllowed && result.RetryAfter > TimeSpan.Zero)
        {
            response.Headers["Retry-After"] = ((int)result.RetryAfter.TotalSeconds).ToString();
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "rate_limit_exceeded",
            message = "Rate limit exceeded. Too many requests.",
            details = new
            {
                limit = result.Limit,
                remaining = result.Remaining,
                reset_time = result.ResetTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                retry_after_seconds = (int)result.RetryAfter.TotalSeconds,
                client_id = result.ClientId,
                endpoint = result.Endpoint,
                auth_level = result.AuthLevel.ToString()
            }
        };

        _logger.LogWarning(
            "Rate limit exceeded for {ClientId} on endpoint {Endpoint} with auth level {AuthLevel}. " +
            "Limit: {Limit}, Reset time: {ResetTime}",
            result.ClientId,
            result.Endpoint,
            result.AuthLevel,
            result.Limit,
            result.ResetTime);

        var json = System.Text.Json.JsonSerializer.Serialize(errorResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}