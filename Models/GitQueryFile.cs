using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class GitQueryFile
{
    public int Id { get; set; }

    [Required]
    public int GitRepositoryId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? SqlContent { get; set; }

    [MaxLength(50)]
    public string? DatabaseType { get; set; }

    public List<string>? Tags { get; set; }

    public string? LastCommitSha { get; set; }

    [MaxLength(100)]
    public string? LastCommitAuthor { get; set; }

    public DateTime LastCommitAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public GitRepository GitRepository { get; set; } = null!;
}