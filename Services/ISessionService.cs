using JoineryServer.Models;

namespace JoineryServer.Services;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(int userId, string ipAddress, string userAgent, string loginMethod, string? deviceInfo = null);
    Task<Session?> GetSessionAsync(string sessionId);
    Task<List<Session>> GetUserActiveSessionsAsync(int userId);
    Task<List<Session>> GetUserAllSessionsAsync(int userId);
    Task<bool> IsSessionValidAsync(string sessionId);
    Task UpdateSessionActivityAsync(string sessionId);
    Task RevokeSessionAsync(string sessionId, string reason, string? revokedByIp = null);
    Task RevokeUserSessionsAsync(int userId, string reason, string? revokedByIp = null);
    Task RevokeOtherUserSessionsAsync(string currentSessionId, int userId, string reason);
    Task<bool> CanCreateNewSessionAsync(int userId);
    Task<List<Session>> GetSuspiciousSessionsAsync();
    Task DetectAnomalousActivityAsync(string sessionId, string ipAddress, string userAgent);
    string GenerateSessionId();
}