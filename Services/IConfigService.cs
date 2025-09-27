namespace JoineryServer.Services;

public interface IConfigService
{
    string GetJwtSecretKey();
    string GetJwtIssuer();
    string GetJwtAudience();
    int GetJwtExpirationHours();
    int GetJwtAccessTokenExpirationMinutes();
    int GetJwtRefreshTokenExpirationDays();
    string GetGitHubClientId();
    string GetGitHubClientSecret();
    bool IsHttpsRequired();
}