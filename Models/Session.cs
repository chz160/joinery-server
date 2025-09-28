namespace JoineryServer.Models;

public class Session
{
    public int Id { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public string? RevokedReason { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    // Session metadata
    public string? LoginMethod { get; set; } // "GitHub", "Microsoft", etc.

    public int ActivityCount { get; set; } = 0;

    public string? Location { get; set; }

    // Anomaly detection flags
    public bool IsSuspicious { get; set; } = false;

    public string? SuspiciousReasons { get; set; }

    // Navigation property
    public User User { get; set; } = null!;

    // Helper properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValidSession => IsActive && !IsExpired;
    public TimeSpan IdleTime => DateTime.UtcNow - LastActivityAt;
}