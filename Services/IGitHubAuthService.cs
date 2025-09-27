using JoineryServer.Models;

namespace JoineryServer.Services;

public interface IGitHubAuthService
{
    Task<GitHubUserInfo?> GetUserInfoAsync(string accessToken);
    string GenerateState();
    bool ValidateState(string storedState, string receivedState);
}

public class GitHubUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}