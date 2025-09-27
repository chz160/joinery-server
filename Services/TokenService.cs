using JoineryServer.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JoineryServer.Services;

public class TokenService : ITokenService
{
    private readonly IConfigService _configService;
    private readonly ILogger<TokenService> _logger;
    private static readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

    public TokenService(IConfigService configService, ILogger<TokenService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var secretKey = _configService.GetJwtSecretKey();
        var issuer = _configService.GetJwtIssuer();
        var audience = _configService.GetJwtAudience();
        var expirationHours = _configService.GetJwtExpirationHours();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("auth_provider", user.AuthProvider),
            new Claim("external_id", user.ExternalId)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(expirationHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);

        // Store refresh token info (in production, use a database)
        _refreshTokens[refreshToken] = new RefreshTokenInfo
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 days for refresh token
        };

        return refreshToken;
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            return tokenInfo.ExpiresAt > DateTime.UtcNow;
        }
        return false;
    }

    public Task<string?> RefreshAccessTokenAsync(string refreshToken)
    {
        if (!ValidateRefreshToken(refreshToken))
        {
            return Task.FromResult<string?>(null);
        }

        // In a real implementation, you would look up the user associated with this refresh token
        // For now, returning null as this requires additional database schema changes
        _logger.LogWarning("Refresh token functionality requires additional implementation");
        return Task.FromResult<string?>(null);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken)
    {
        _refreshTokens.Remove(refreshToken);
        return Task.CompletedTask;
    }

    private class RefreshTokenInfo
    {
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}