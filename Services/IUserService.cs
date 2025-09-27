using JoineryServer.Models;

namespace JoineryServer.Services;

public interface IUserService
{
    Task<User> GetOrCreateUserAsync(string externalId, string username, string email, string authProvider, string? fullName = null);
    Task<User?> GetUserByExternalIdAsync(string externalId, string authProvider);
    Task UpdateLastLoginAsync(int userId);
}