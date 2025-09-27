using JoineryServer.Models;

namespace JoineryServer.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string refreshToken);
    Task<string?> RefreshAccessTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
}