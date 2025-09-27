using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JoineryServer.Services;
using System.Security.Claims;

namespace JoineryServer.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active sessions for the current user
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetCurrentUserId();
        var sessions = await _sessionService.GetUserActiveSessionsAsync(userId);

        var sessionData = sessions.Select(s => new
        {
            s.SessionId,
            s.DeviceInfo,
            s.IpAddress,
            s.Location,
            s.LoginMethod,
            s.CreatedAt,
            s.LastActivityAt,
            s.ActivityCount,
            s.IsSuspicious,
            IsCurrent = IsCurrentSession(s.SessionId)
        }).ToList();

        return Ok(sessionData);
    }

    /// <summary>
    /// Get all sessions (including inactive) for the current user
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllSessions()
    {
        var userId = GetCurrentUserId();
        var sessions = await _sessionService.GetUserAllSessionsAsync(userId);

        var sessionData = sessions.Select(s => new
        {
            s.SessionId,
            s.DeviceInfo,
            s.IpAddress,
            s.Location,
            s.LoginMethod,
            s.CreatedAt,
            s.LastActivityAt,
            s.ExpiresAt,
            s.IsActive,
            s.ActivityCount,
            s.IsSuspicious,
            s.RevokedReason,
            s.RevokedAt,
            IsCurrent = IsCurrentSession(s.SessionId)
        }).ToList();

        return Ok(sessionData);
    }

    /// <summary>
    /// Get details of a specific session
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        var userId = GetCurrentUserId();
        if (session.UserId != userId)
        {
            return Forbid("You can only view your own sessions");
        }

        var sessionData = new
        {
            session.SessionId,
            session.DeviceInfo,
            session.IpAddress,
            session.UserAgent,
            session.Location,
            session.LoginMethod,
            session.CreatedAt,
            session.LastActivityAt,
            session.ExpiresAt,
            session.IsActive,
            session.ActivityCount,
            session.IsSuspicious,
            session.SuspiciousReasons,
            session.RevokedReason,
            session.RevokedAt,
            IsCurrent = IsCurrentSession(session.SessionId)
        };

        return Ok(sessionData);
    }

    /// <summary>
    /// Revoke a specific session
    /// </summary>
    [HttpPost("{sessionId}/revoke")]
    public async Task<IActionResult> RevokeSession(string sessionId, [FromBody] RevokeSessionRequest? request = null)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);

        if (session == null)
        {
            return NotFound("Session not found");
        }

        var userId = GetCurrentUserId();
        if (session.UserId != userId)
        {
            return Forbid("You can only revoke your own sessions");
        }

        var reason = request?.Reason ?? "Revoked by user";
        var clientIp = GetClientIpAddress();

        await _sessionService.RevokeSessionAsync(sessionId, reason, clientIp);

        _logger.LogInformation("User {UserId} revoked session {SessionId}", userId, sessionId);

        return Ok(new { message = "Session revoked successfully" });
    }

    /// <summary>
    /// Revoke all other sessions (keep only the current one)
    /// </summary>
    [HttpPost("revoke-others")]
    public async Task<IActionResult> RevokeOtherSessions([FromBody] RevokeSessionRequest? request = null)
    {
        var userId = GetCurrentUserId();
        var currentSessionId = GetCurrentSessionId();

        if (string.IsNullOrEmpty(currentSessionId))
        {
            return BadRequest("Current session ID not found");
        }

        var reason = request?.Reason ?? "Other sessions revoked by user";

        await _sessionService.RevokeOtherUserSessionsAsync(currentSessionId, userId, reason);

        _logger.LogInformation("User {UserId} revoked all other sessions", userId);

        return Ok(new { message = "Other sessions revoked successfully" });
    }

    /// <summary>
    /// Revoke all sessions for the current user
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllSessions([FromBody] RevokeSessionRequest? request = null)
    {
        var userId = GetCurrentUserId();
        var reason = request?.Reason ?? "All sessions revoked by user";
        var clientIp = GetClientIpAddress();

        await _sessionService.RevokeUserSessionsAsync(userId, reason, clientIp);

        _logger.LogInformation("User {UserId} revoked all sessions", userId);

        return Ok(new { message = "All sessions revoked successfully" });
    }

    /// <summary>
    /// Get suspicious sessions (admin only)
    /// </summary>
    [HttpGet("suspicious")]
    public async Task<IActionResult> GetSuspiciousSessions()
    {
        // For now, any authenticated user can see suspicious sessions for their account
        // In a real implementation, this might be admin-only
        var userId = GetCurrentUserId();
        var allSuspicious = await _sessionService.GetSuspiciousSessionsAsync();
        var userSuspicious = allSuspicious.Where(s => s.UserId == userId).ToList();

        var sessionData = userSuspicious.Select(s => new
        {
            s.SessionId,
            s.DeviceInfo,
            s.IpAddress,
            s.Location,
            s.LoginMethod,
            s.CreatedAt,
            s.LastActivityAt,
            s.SuspiciousReasons,
            s.ActivityCount
        }).ToList();

        return Ok(sessionData);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    private string? GetCurrentSessionId()
    {
        return HttpContext.Items["SessionId"] as string;
    }

    private bool IsCurrentSession(string sessionId)
    {
        var currentSessionId = GetCurrentSessionId();
        return !string.IsNullOrEmpty(currentSessionId) && currentSessionId == sessionId;
    }

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class RevokeSessionRequest
{
    public string? Reason { get; set; }
}