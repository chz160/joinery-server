using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class GitRepository
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string RepositoryUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Branch { get; set; } = "main";

    [MaxLength(200)]
    public string? AccessToken { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Repository scope - either Organization or Team level
    public int? OrganizationId { get; set; }
    public int? TeamId { get; set; }

    [Required]
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