namespace JoineryServer.Models;

public class BlacklistedToken
{
    public int Id { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public string? BlacklistedByIp { get; set; }

    public string? Reason { get; set; }

    // Type of token (access, refresh)
    public string TokenType { get; set; } = string.Empty;

    public int? UserId { get; set; }

    // Navigation property
    public User? User { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}