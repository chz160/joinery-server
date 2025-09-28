namespace JoineryServer.Models;

public class DatabaseQuery
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SqlQuery { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public string? DatabaseType { get; set; }

    public List<string>? Tags { get; set; }
}