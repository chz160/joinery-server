using JoineryServer.Models;

namespace JoineryServer.Services;

public interface ISessionRepository
{
    Task<Session> CreateSessionAsync(Session session);
    Task<Session?> GetSessionByIdAsync(string sessionId);
    Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId);
    Task<List<Session>> GetAllSessionsByUserIdAsync(int userId);
    Task<Session> UpdateSessionAsync(Session session);
    Task RevokeSessionAsync(string sessionId, string reason, string? revokedByIp = null);
    Task RevokeUserSessionsAsync(int userId, string reason, string? revokedByIp = null);
    Task<List<Session>> GetExpiredSessionsAsync();
    Task DeleteSessionsAsync(List<string> sessionIds);
    Task<int> GetActiveSessionCountByUserIdAsync(int userId);
    Task<List<Session>> GetSuspiciousSessionsAsync();
    Task MarkSessionSuspiciousAsync(string sessionId, string reason);
}