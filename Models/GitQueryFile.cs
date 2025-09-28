namespace JoineryServer.Models;

public class GitQueryFile
{
    public int Id { get; set; }

    public int GitRepositoryId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? SqlContent { get; set; }

    public string? DatabaseType { get; set; }

    public List<string>? Tags { get; set; }

    public string? LastCommitSha { get; set; }

    public string? LastCommitAuthor { get; set; }

    public DateTime LastCommitAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public GitRepository GitRepository { get; set; } = null!;
}