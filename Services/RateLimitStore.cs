using JoineryServer.Models;
using System.Collections.Concurrent;

namespace JoineryServer.Services;

public interface IRateLimitStore
{
    Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, UserAuthLevel authLevel, RateLimitSettings settings);
}

/// <summary>
/// In-memory rate limit store implementation with sliding window
/// </summary>
public class MemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits = new();
    private readonly ILogger<MemoryRateLimitStore> _logger;
    private readonly Timer _cleanupTimer;

    public MemoryRateLimitStore(ILogger<MemoryRateLimitStore> logger)
    {
        _logger = logger;
        // Run cleanup every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, UserAuthLevel authLevel, RateLimitSettings settings)
    {
        var key = $"{clientId}:{endpoint}:{authLevel}";
        var now = DateTime.UtcNow;

        var clientLimit = _clientLimits.GetOrAdd(key, _ => new ClientRateLimit());

        lock (clientLimit)
        {
            // Clean old requests
            CleanupOldRequests(clientLimit, now);

            // Get effective limit (0 means unlimited)
            var limit = GetEffectiveLimit(settings, now);

            if (limit == 0)
            {
                return Task.FromResult(new RateLimitResult
                {
                    IsAllowed = true,
                    Remaining = int.MaxValue,
                    Limit = 0,
                    ResetTime = now.AddMinutes(1),
                    ClientId = clientId,
                    Endpoint = endpoint,
                    AuthLevel = authLevel,
                    RetryAfter = TimeSpan.Zero
                });
            }

            var requestCount = GetRequestCount(clientLimit, now, out DateTime resetTime);
            var remaining = Math.Max(0, limit - requestCount);

            if (requestCount >= limit)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}, endpoint {Endpoint}, auth level {AuthLevel}. Count: {Count}, Limit: {Limit}",
                    clientId, endpoint, authLevel, requestCount, limit);

                return Task.FromResult(new RateLimitResult
                {
                    IsAllowed = false,
                    Remaining = 0,
                    Limit = limit,
                    ResetTime = resetTime,
                    ClientId = clientId,
                    Endpoint = endpoint,
                    AuthLevel = authLevel,
                    RetryAfter = resetTime - now
                });
            }

            // Record this request
            clientLimit.Requests.Add(now);

            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = true,
                Remaining = remaining - 1, // Account for current request
                Limit = limit,
                ResetTime = resetTime,
                ClientId = clientId,
                Endpoint = endpoint,
                AuthLevel = authLevel,
                RetryAfter = TimeSpan.Zero
            });
        }
    }

    private void CleanupOldRequests(ClientRateLimit clientLimit, DateTime now)
    {
        // Remove requests older than 24 hours (covers daily limits)
        clientLimit.Requests.RemoveAll(r => (now - r).TotalHours > 24);
    }

    private int GetEffectiveLimit(RateLimitSettings settings, DateTime now)
    {
        // For sliding window, we primarily use per-minute limits
        // The other limits are for compliance but shouldn't be more restrictive than per-minute
        return settings.RequestsPerMinute;
    }

    private int GetRequestCount(ClientRateLimit clientLimit, DateTime now, out DateTime resetTime)
    {
        // Count requests in the last minute for minute-based limiting
        var oneMinuteAgo = now.AddMinutes(-1);
        var recentRequests = clientLimit.Requests.Where(r => r > oneMinuteAgo).ToList();

        resetTime = recentRequests.Any()
            ? recentRequests.Min().AddMinutes(1)
            : now.AddMinutes(1);

        return recentRequests.Count;
    }

    private void CleanupExpiredEntries(object? state)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var keysToRemove = new List<string>();

        foreach (var kvp in _clientLimits)
        {
            lock (kvp.Value)
            {
                kvp.Value.Requests.RemoveAll(r => r < cutoff);
                if (!kvp.Value.Requests.Any())
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _clientLimits.TryRemove(key, out _);
        }

        _logger.LogDebug("Rate limit cleanup removed {Count} expired entries", keysToRemove.Count);
    }

    private class ClientRateLimit
    {
        public List<DateTime> Requests { get; } = new();
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Redis-based distributed rate limit store (placeholder for future implementation)
/// </summary>
public class RedisRateLimitStore : IRateLimitStore
{
    private readonly ILogger<RedisRateLimitStore> _logger;
    private readonly MemoryRateLimitStore _fallbackStore;

    public RedisRateLimitStore(ILogger<RedisRateLimitStore> logger, MemoryRateLimitStore fallbackStore)
    {
        _logger = logger;
        _fallbackStore = fallbackStore;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, UserAuthLevel authLevel, RateLimitSettings settings)
    {
        try
        {
            // TODO: Implement Redis-based rate limiting
            // For now, fall back to memory store
            _logger.LogWarning("Redis rate limiting not implemented, falling back to memory store");
            return await _fallbackStore.CheckRateLimitAsync(clientId, endpoint, authLevel, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis rate limiting failed, falling back to memory store");
            return await _fallbackStore.CheckRateLimitAsync(clientId, endpoint, authLevel, settings);
        }
    }
}