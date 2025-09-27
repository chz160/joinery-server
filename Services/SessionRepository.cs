using JoineryServer.Data;
using JoineryServer.Models;
using Microsoft.EntityFrameworkCore;

namespace JoineryServer.Services;

public class SessionRepository : ISessionRepository
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<SessionRepository> _logger;

    public SessionRepository(JoineryDbContext context, ILogger<SessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created new session {SessionId} for user {UserId}", session.SessionId, session.UserId);
        return session;
    }

    public async Task<Session?> GetSessionByIdAsync(string sessionId)
    {
        return await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    public async Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId)
    {
        return await _context.Sessions
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task<List<Session>> GetAllSessionsByUserIdAsync(int userId)
    {
        return await _context.Sessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Session> UpdateSessionAsync(Session session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task RevokeSessionAsync(string sessionId, string reason, string? revokedByIp = null)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session != null)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
            session.RevokedByIp = revokedByIp;
            await UpdateSessionAsync(session);
            _logger.LogInformation("Revoked session {SessionId} for user {UserId}: {Reason}", sessionId, session.UserId, reason);
        }
    }

    public async Task RevokeUserSessionsAsync(int userId, string reason, string? revokedByIp = null)
    {
        var sessions = await GetActiveSessionsByUserIdAsync(userId);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
            session.RevokedByIp = revokedByIp;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Revoked {Count} sessions for user {UserId}: {Reason}", sessions.Count, userId, reason);
    }

    public async Task<List<Session>> GetExpiredSessionsAsync()
    {
        return await _context.Sessions
            .Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task DeleteSessionsAsync(List<string> sessionIds)
    {
        var sessions = await _context.Sessions
            .Where(s => sessionIds.Contains(s.SessionId))
            .ToListAsync();

        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted {Count} sessions", sessions.Count);
    }

    public async Task<int> GetActiveSessionCountByUserIdAsync(int userId)
    {
        return await _context.Sessions
            .CountAsync(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<List<Session>> GetSuspiciousSessionsAsync()
    {
        return await _context.Sessions
            .Where(s => s.IsSuspicious && s.IsActive)
            .Include(s => s.User)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task MarkSessionSuspiciousAsync(string sessionId, string reason)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session != null)
        {
            session.IsSuspicious = true;
            session.SuspiciousReasons = string.IsNullOrEmpty(session.SuspiciousReasons)
                ? reason
                : $"{session.SuspiciousReasons}; {reason}";
            await UpdateSessionAsync(session);
            _logger.LogWarning("Marked session {SessionId} as suspicious for user {UserId}: {Reason}",
                sessionId, session.UserId, reason);
        }
    }
}