using JoineryServer.Models;
using System.Text.Json;

namespace JoineryServer.Services;

public class GitHubAuthService : IGitHubAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubAuthService> _logger;

    public GitHubAuthService(HttpClient httpClient, ILogger<GitHubAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GitHubUserInfo?> GetUserInfoAsync(string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "JoineryServer/1.0");

            var response = await _httpClient.GetAsync("https://api.github.com/user");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GitHubUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user info from GitHub");
            return null;
        }
    }

    public string GenerateState()
    {
        return Guid.NewGuid().ToString("N");
    }

    public bool ValidateState(string storedState, string receivedState)
    {
        return !string.IsNullOrEmpty(storedState) &&
               !string.IsNullOrEmpty(receivedState) &&
               storedState == receivedState;
    }
}