namespace JoineryServer.Models;

public class ApiKey
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string KeyHash { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public string? LastUsedFromIp { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public string? RevokedByIp { get; set; }

    public int UserId { get; set; }

    // Scopes as a comma-separated string (e.g., "read,write,admin")
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