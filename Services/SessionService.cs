using JoineryServer.Models;
using System.Security.Cryptography;

namespace JoineryServer.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IConfigService _configService;
    private readonly ILogger<SessionService> _logger;

    private const int MaxConcurrentSessions = 5; // Default limit
    private const int SessionExpirationHours = 24; // Default session expiration
    private const int SessionIdleTimeoutMinutes = 120; // Default idle timeout

    public SessionService(
        ISessionRepository sessionRepository,
        IConfigService configService,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _configService = configService;
        _logger = logger;
    }

    public async Task<Session> CreateSessionAsync(int userId, string ipAddress, string userAgent,
        string loginMethod, string? deviceInfo = null)
    {
        // Check if user can create new session (concurrent session limit)
        if (!await CanCreateNewSessionAsync(userId))
        {
            _logger.LogWarning("User {UserId} exceeded concurrent session limit", userId);
            throw new InvalidOperationException("Maximum concurrent sessions exceeded");
        }

        var sessionId = GenerateSessionId();
        var expirationTime = DateTime.UtcNow.AddHours(SessionExpirationHours);

        var session = new Session
        {
            SessionId = sessionId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceInfo = deviceInfo,
            LoginMethod = loginMethod,
            ExpiresAt = expirationTime,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        var createdSession = await _sessionRepository.CreateSessionAsync(session);

        _logger.LogInformation("Created session {SessionId} for user {UserId} from {IpAddress}",
            sessionId, userId, ipAddress);

        return createdSession;
    }

    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        return await _sessionRepository.GetSessionByIdAsync(sessionId);
    }

    public async Task<List<Session>> GetUserActiveSessionsAsync(int userId)
    {
        return await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
    }

    public async Task<List<Session>> GetUserAllSessionsAsync(int userId)
    {
        return await _sessionRepository.GetAllSessionsByUserIdAsync(userId);
    }

    public async Task<bool> IsSessionValidAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return false;

        // Check if session is active and not expired
        if (!session.IsValidSession) return false;

        // Check idle timeout
        if (session.IdleTime.TotalMinutes > SessionIdleTimeoutMinutes)
        {
            await RevokeSessionAsync(sessionId, "Session idle timeout exceeded");
            return false;
        }

        return true;
    }

    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session != null && session.IsValidSession)
        {
            session.LastActivityAt = DateTime.UtcNow;
            session.ActivityCount++;
            await _sessionRepository.UpdateSessionAsync(session);
        }
    }

    public async Task RevokeSessionAsync(string sessionId, string reason, string? revokedByIp = null)
    {
        await _sessionRepository.RevokeSessionAsync(sessionId, reason, revokedByIp);
    }

    public async Task RevokeUserSessionsAsync(int userId, string reason, string? revokedByIp = null)
    {
        await _sessionRepository.RevokeUserSessionsAsync(userId, reason, revokedByIp);
    }

    public async Task RevokeOtherUserSessionsAsync(string currentSessionId, int userId, string reason)
    {
        var sessions = await GetUserActiveSessionsAsync(userId);
        var otherSessions = sessions.Where(s => s.SessionId != currentSessionId).ToList();

        foreach (var session in otherSessions)
        {
            await RevokeSessionAsync(session.SessionId, reason);
        }

        _logger.LogInformation("Revoked {Count} other sessions for user {UserId}",
            otherSessions.Count, userId);
    }

    public async Task<bool> CanCreateNewSessionAsync(int userId)
    {
        var activeSessionCount = await _sessionRepository.GetActiveSessionCountByUserIdAsync(userId);
        return activeSessionCount < MaxConcurrentSessions;
    }

    public async Task<List<Session>> GetSuspiciousSessionsAsync()
    {
        return await _sessionRepository.GetSuspiciousSessionsAsync();
    }

    public async Task DetectAnomalousActivityAsync(string sessionId, string ipAddress, string userAgent)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        var reasons = new List<string>();

        // Check IP address change
        if (!string.IsNullOrEmpty(session.IpAddress) && session.IpAddress != ipAddress)
        {
            reasons.Add($"IP address changed from {session.IpAddress} to {ipAddress}");
        }

        // Check user agent change
        if (!string.IsNullOrEmpty(session.UserAgent) && session.UserAgent != userAgent)
        {
            reasons.Add($"User agent changed");
        }

        // Check unusual activity patterns
        var userSessions = await GetUserActiveSessionsAsync(session.UserId);
        var recentSessions = userSessions.Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-1)).ToList();

        if (recentSessions.Count > 3)
        {
            reasons.Add("Multiple concurrent sessions created within short timeframe");
        }

        // Check for rapid activity
        if (session.ActivityCount > 100 && session.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
        {
            reasons.Add("Unusually high activity rate");
        }

        if (reasons.Any())
        {
            var reasonsText = string.Join("; ", reasons);
            await _sessionRepository.MarkSessionSuspiciousAsync(sessionId, reasonsText);

            _logger.LogWarning("Detected suspicious activity for session {SessionId} (User {UserId}): {Reasons}",
                sessionId, session.UserId, reasonsText);
        }
    }

    public string GenerateSessionId()
    {
        // Generate cryptographically secure session ID
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}