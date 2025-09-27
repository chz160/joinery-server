using System.Collections.Concurrent;

namespace JoineryServer.Services;

public interface IRateLimitingService
{
    bool IsAllowed(string clientId, string endpoint);
}

public class RateLimitingService : IRateLimitingService
{
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits = new();
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(ILogger<RateLimitingService> logger)
    {
        _logger = logger;
    }

    public bool IsAllowed(string clientId, string endpoint)
    {
        var key = $"{clientId}:{endpoint}";
        var now = DateTime.UtcNow;

        var limit = _clientLimits.GetOrAdd(key, _ => new ClientRateLimit());

        // Remove old requests (older than 1 minute)
        limit.Requests.RemoveAll(r => (now - r).TotalMinutes > 1);

        // Check if we've exceeded the limit (10 requests per minute for auth endpoints)
        if (limit.Requests.Count >= 10)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
            return false;
        }

        // Add current request
        limit.Requests.Add(now);
        return true;
    }

    private class ClientRateLimit
    {
        public List<DateTime> Requests { get; } = new();
    }
}