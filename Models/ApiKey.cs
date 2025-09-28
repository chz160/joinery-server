using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class ApiKey
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(1000)]
    public string KeyHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    [MaxLength(100)]
    public string? LastUsedFromIp { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? RevokedReason { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    [Required]
    public int UserId { get; set; }

    // Scopes as a comma-separated string (e.g., "read,write,admin")
    [MaxLength(1000)]
    public string Scopes { get; set; } = string.Empty;

    // Navigation property
    public User User { get; set; } = null!;

    // Helper properties
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

    public bool IsValid => IsActive && !IsRevoked && !IsExpired;

    public List<string> GetScopes()
    {
        return string.IsNullOrEmpty(Scopes)
            ? new List<string>()
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
    }

    public void SetScopes(IEnumerable<string> scopes)
    {
        Scopes = string.Join(',', scopes);
    }
}