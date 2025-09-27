using JoineryServer.Models;
using JoineryServer.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JoineryServer.Services;

public class TokenService : ITokenService
{
    private readonly IConfigService _configService;
    private readonly ILogger<TokenService> _logger;
    private readonly JoineryDbContext _context;

    public TokenService(IConfigService configService, ILogger<TokenService> logger, JoineryDbContext context)
    {
        _configService = configService;
        _logger = logger;
        _context = context;
    }

    public string GenerateAccessToken(User user)
    {
        var secretKey = _configService.GetJwtSecretKey();
        var issuer = _configService.GetJwtIssuer();
        var audience = _configService.GetJwtAudience();
        var expirationMinutes = _configService.GetJwtAccessTokenExpirationMinutes();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("auth_provider", user.AuthProvider),
            new Claim("external_id", user.ExternalId),
            new Claim("user_version", "1") // For token versioning
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(int userId)
    {
        var refreshTokenExpirationDays = _configService.GetJwtRefreshTokenExpirationDays();

        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            Version = 1
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated refresh token for user {UserId}", userId);
        return refreshToken;
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var token = _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefault(rt => rt.Token == refreshToken);

        return token?.IsActive == true;
    }

    public async Task<(string? AccessToken, int? UserId)> RefreshAccessTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh token is null or empty");
            return (null, null);
        }

        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token?.IsActive != true)
        {
            _logger.LogWarning("Invalid or inactive refresh token");
            return (null, null);
        }

        // Check if token is blacklisted
        if (await IsTokenBlacklistedAsync(refreshToken, "refresh"))
        {
            _logger.LogWarning("Attempted to use blacklisted refresh token");
            return (null, null);
        }

        // Generate new access token
        var newAccessToken = GenerateAccessToken(token.User);

        // Update last login time
        token.User.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refreshed access token for user {UserId}", token.UserId);
        return (newAccessToken, token.UserId);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? reason = null, string? revokedByIp = null)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = reason ?? "Manual revocation";
            token.RevokedByIp = revokedByIp;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked refresh token for user {UserId}", token.UserId);
        }
    }

    public async Task RevokeAllUserTokensAsync(int userId, string? reason = null, string? revokedByIp = null)
    {
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = reason ?? "Mass token revocation";
            token.RevokedByIp = revokedByIp;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Revoked all {Count} refresh tokens for user {UserId}", userTokens.Count, userId);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, string tokenType)
    {
        if (string.IsNullOrEmpty(token))
            return true;

        var tokenHash = GetTokenHash(token);
        
        var blacklistedToken = await _context.BlacklistedTokens
            .FirstOrDefaultAsync(bt => bt.TokenHash == tokenHash && bt.TokenType == tokenType);

        return blacklistedToken != null && !blacklistedToken.IsExpired;
    }

    public async Task BlacklistTokenAsync(string token, string tokenType, int? userId = null, string? reason = null, string? blacklistedByIp = null)
    {
        if (string.IsNullOrEmpty(token))
            return;

        var tokenHash = GetTokenHash(token);
        
        // Check if already blacklisted
        var existingBlacklist = await _context.BlacklistedTokens
            .FirstOrDefaultAsync(bt => bt.TokenHash == tokenHash && bt.TokenType == tokenType);

        if (existingBlacklist != null)
            return;

        // Determine expiration date based on token type
        DateTime expiresAt;
        if (tokenType == "access")
        {
            expiresAt = DateTime.UtcNow.AddMinutes(_configService.GetJwtAccessTokenExpirationMinutes());
        }
        else if (tokenType == "refresh")
        {
            expiresAt = DateTime.UtcNow.AddDays(_configService.GetJwtRefreshTokenExpirationDays());
        }
        else
        {
            expiresAt = DateTime.UtcNow.AddDays(30); // Default
        }

        var blacklistedToken = new BlacklistedToken
        {
            TokenHash = tokenHash,
            TokenType = tokenType,
            UserId = userId,
            ExpiresAt = expiresAt,
            Reason = reason ?? "Token blacklisted",
            BlacklistedByIp = blacklistedByIp
        };

        _context.BlacklistedTokens.Add(blacklistedToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Blacklisted {TokenType} token for user {UserId}", tokenType, userId);
    }

    public string GetTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}