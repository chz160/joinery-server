using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class BlacklistedToken
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    [MaxLength(100)]
    public string? BlacklistedByIp { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    // Type of token (access, refresh)
    [Required]
    [MaxLength(20)]
    public string TokenType { get; set; } = string.Empty;

    public int? UserId { get; set; }

    // Navigation property
    public User? User { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}