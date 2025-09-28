using JoineryServer.Data;
using JoineryServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JoineryServer.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;
    private const string KEY_PREFIX = "jsk_"; // Joinery Server Key
    private const int KEY_LENGTH = 32; // bytes

    public ApiKeyService(JoineryDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(ApiKey apiKey, string rawKey)> GenerateApiKeyAsync(
        int userId,
        string name,
        string? description = null,
        DateTime? expiresAt = null,
        IEnumerable<string>? scopes = null)
    {
        // Generate cryptographically secure API key
        var rawKey = GenerateSecureApiKey();
        var keyHash = GetKeyHash(rawKey);
        var keyPrefix = rawKey.Substring(0, KEY_PREFIX.Length + 8); // Include first 8 chars after prefix for identification

        var apiKey = new ApiKey
        {
            Name = name,
            Description = description,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            UserId = userId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Scopes = scopes != null ? string.Join(',', scopes) : "read"
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated API key {KeyPrefix}... for user {UserId} with name '{Name}'",
            keyPrefix, userId, name);

        return (apiKey, rawKey);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string apiKey)
    {
        if (!ValidateKeyFormat(apiKey))
        {
            return null;
        }

        var keyHash = GetKeyHash(apiKey);

        var foundKey = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);

        if (foundKey == null || !foundKey.IsValid)
        {
            return null;
        }

        return foundKey;
    }

    public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string reason, string? revokedByIp = null)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.IsRevoked = true;
        apiKey.RevokedAt = DateTime.UtcNow;
        apiKey.RevokedReason = reason;
        apiKey.RevokedByIp = revokedByIp;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked API key {KeyPrefix}... for user {UserId}: {Reason}",
            apiKey.KeyPrefix, apiKey.UserId, reason);

        return true;
    }

    public async Task<bool> UpdateLastUsedAsync(int apiKeyId, string? ipAddress = null)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey == null)
        {
            return false;
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(ipAddress))
        {
            apiKey.LastUsedFromIp = ipAddress;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApiKey>> GetUserApiKeysAsync(int userId)
    {
        return await _context.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKey?> GetApiKeyByIdAsync(int apiKeyId)
    {
        return await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == apiKeyId);
    }

    public bool ValidateKeyFormat(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return false;

        // Check if key starts with our prefix
        if (!apiKey.StartsWith(KEY_PREFIX))
            return false;

        // Check if key has reasonable length (prefix + base64 encoded key)
        var expectedMinLength = KEY_PREFIX.Length + ((KEY_LENGTH * 4 / 3) + 3) & ~3; // Base64 padding
        if (apiKey.Length < expectedMinLength)
            return false;

        return true;
    }

    public string GetKeyHash(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateSecureApiKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[KEY_LENGTH];
        rng.GetBytes(keyBytes);

        var base64Key = Convert.ToBase64String(keyBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        return KEY_PREFIX + base64Key;
    }
}