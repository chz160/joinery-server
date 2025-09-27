using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class Session
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    [MaxLength(200)]
    public string? DeviceInfo { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? RevokedReason { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    // Session metadata
    [MaxLength(50)]
    public string? LoginMethod { get; set; } // "GitHub", "Microsoft", etc.

    public int ActivityCount { get; set; } = 0;

    [MaxLength(100)]
    public string? Location { get; set; }

    // Anomaly detection flags
    public bool IsSuspicious { get; set; } = false;

    [MaxLength(1000)]
    public string? SuspiciousReasons { get; set; }

    // Navigation property
    public User User { get; set; } = null!;

    // Helper properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValidSession => IsActive && !IsExpired;
    public TimeSpan IdleTime => DateTime.UtcNow - LastActivityAt;
}