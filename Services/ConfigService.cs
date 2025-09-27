namespace JoineryServer.Services;

public class ConfigService : IConfigService
{
    private readonly IConfiguration _configuration;

    public ConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetJwtSecretKey()
    {
        return _configuration.GetSection("JWT")["SecretKey"] ?? "default-secret-key";
    }

    public string GetJwtIssuer()
    {
        return _configuration.GetSection("JWT")["Issuer"] ?? "JoineryServer";
    }

    public string GetJwtAudience()
    {
        return _configuration.GetSection("JWT")["Audience"] ?? "JoineryClients";
    }

    public int GetJwtExpirationHours()
    {
        return int.Parse(_configuration.GetSection("JWT")["ExpirationHours"] ?? "24");
    }

    public int GetJwtAccessTokenExpirationMinutes()
    {
        return int.Parse(_configuration.GetSection("JWT")["AccessTokenExpirationMinutes"] ?? "15");
    }

    public int GetJwtRefreshTokenExpirationDays()
    {
        return int.Parse(_configuration.GetSection("JWT")["RefreshTokenExpirationDays"] ?? "30");
    }

    public string GetGitHubClientId()
    {
        return _configuration.GetSection("Authentication:GitHub")["ClientId"] ?? "";
    }

    public string GetGitHubClientSecret()
    {
        return _configuration.GetSection("Authentication:GitHub")["ClientSecret"] ?? "";
    }

    public bool IsHttpsRequired()
    {
        return !_configuration.GetValue<bool>("ASPNETCORE_ENVIRONMENT", "Production".Equals("Development", StringComparison.OrdinalIgnoreCase));
    }
}