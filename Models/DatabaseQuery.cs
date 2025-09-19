using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class DatabaseQuery
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SqlQuery { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    [MaxLength(50)]
    public string? DatabaseType { get; set; }

    public List<string>? Tags { get; set; }
}