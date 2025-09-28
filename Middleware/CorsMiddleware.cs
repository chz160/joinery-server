using JoineryServer.Models;
using Microsoft.Extensions.Options;

namespace JoineryServer.Middleware;

public class CorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorsConfig _corsConfig;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CorsMiddleware> _logger;

    public CorsMiddleware(
        RequestDelegate next,
        IOptions<CorsConfig> corsConfig,
        IHostEnvironment environment,
        ILogger<CorsMiddleware> logger)
    {
        _next = next;
        _corsConfig = corsConfig.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip CORS handling if CORS is disabled
        if (!_corsConfig.EnableCors)
        {
            await _next(context);
            return;
        }

        var origin = context.Request.Headers["Origin"].FirstOrDefault();

        if (!string.IsNullOrEmpty(origin))
        {
            if (IsOriginAllowed(origin))
            {
                AddCorsHeaders(context.Response, origin);
            }
            else
            {
                _logger.LogWarning("CORS request from unauthorized origin: {Origin}", origin);
                // Don't add CORS headers for unauthorized origins
            }
        }

        // Handle preflight requests
        if (context.Request.Method == "OPTIONS")
        {
            if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
            {
                context.Response.StatusCode = 204; // No Content
                return;
            }
            else
            {
                context.Response.StatusCode = 403; // Forbidden for unauthorized origins
                return;
            }
        }

        await _next(context);
    }

    private bool IsOriginAllowed(string origin)
    {
        // In development, allow any origin if configured to do so
        if (_environment.IsDevelopment() && _corsConfig.AllowAnyOriginInDevelopment)
        {
            return true;
        }

        // Check against configured allowed origins
        return _corsConfig.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private void AddCorsHeaders(HttpResponse response, string origin)
    {
        response.Headers["Access-Control-Allow-Origin"] = origin;

        if (_corsConfig.AllowCredentials)
        {
            response.Headers["Access-Control-Allow-Credentials"] = "true";
        }

        if (_corsConfig.AllowedMethods.Any())
        {
            response.Headers["Access-Control-Allow-Methods"] = string.Join(", ", _corsConfig.AllowedMethods);
        }

        if (_corsConfig.AllowedHeaders.Any())
        {
            response.Headers["Access-Control-Allow-Headers"] = string.Join(", ", _corsConfig.AllowedHeaders);
        }

        if (_corsConfig.ExposedHeaders.Any())
        {
            response.Headers["Access-Control-Expose-Headers"] = string.Join(", ", _corsConfig.ExposedHeaders);
        }

        response.Headers["Access-Control-Max-Age"] = _corsConfig.PreflightMaxAge.ToString();
    }
}