namespace JoineryServer.Services;

public interface IConfigService
{
    string GetJwtSecretKey();
    string GetJwtIssuer();
    string GetJwtAudience();
    int GetJwtExpirationHours();
    string GetGitHubClientId();
    string GetGitHubClientSecret();
    bool IsHttpsRequired();
}