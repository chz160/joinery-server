using JoineryServer.Models;

namespace JoineryServer.Services;

public interface IApiKeyService
{
    Task<(ApiKey apiKey, string rawKey)> GenerateApiKeyAsync(int userId, string name, string? description = null, DateTime? expiresAt = null, IEnumerable<string>? scopes = null);
    Task<ApiKey?> ValidateApiKeyAsync(string apiKey);
    Task<bool> RevokeApiKeyAsync(int apiKeyId, string reason, string? revokedByIp = null);
    Task<bool> UpdateLastUsedAsync(int apiKeyId, string? ipAddress = null);
    Task<List<ApiKey>> GetUserApiKeysAsync(int userId);
    Task<ApiKey?> GetApiKeyByIdAsync(int apiKeyId);
    bool ValidateKeyFormat(string apiKey);
    string GetKeyHash(string apiKey);
}