namespace JoineryServer.Models;

public class GitRepository
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public string? Branch { get; set; } = "main";

    public string? AccessToken { get; set; }

    public string? Description { get; set; }

    // Repository scope - either Organization or Team level
    public int? OrganizationId { get; set; }
    public int? TeamId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public Organization? Organization { get; set; }
    public Team? Team { get; set; }
    public ICollection<GitQueryFile> QueryFiles { get; set; } = new List<GitQueryFile>();
}