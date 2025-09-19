using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class OrganizationAwsIamConfig
{
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string AwsRegion { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AccessKeyId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string SecretAccessKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RoleArn { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}