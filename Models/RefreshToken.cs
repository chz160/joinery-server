using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    [MaxLength(500)]
    public string? ReasonRevoked { get; set; }

    // Token version for forced rotation
    public int Version { get; set; } = 1;

    // Navigation property
    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}