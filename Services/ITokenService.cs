using JoineryServer.Models;

namespace JoineryServer.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateAccessToken(User user, string sessionId);
    Task<RefreshToken> GenerateRefreshTokenAsync(int userId);
    bool ValidateRefreshToken(string refreshToken);
    Task<(string? AccessToken, int? UserId)> RefreshAccessTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken, string? reason = null, string? revokedByIp = null);
    Task RevokeAllUserTokensAsync(int userId, string? reason = null, string? revokedByIp = null);
    Task<bool> IsTokenBlacklistedAsync(string token, string tokenType);
    Task BlacklistTokenAsync(string token, string tokenType, int? userId = null, string? reason = null, string? blacklistedByIp = null);
    string GetTokenHash(string token);
}