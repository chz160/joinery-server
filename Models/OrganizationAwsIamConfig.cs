namespace JoineryServer.Models;

public class OrganizationAwsIamConfig
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public string AwsRegion { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string? RoleArn { get; set; }

    public string? ExternalId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}