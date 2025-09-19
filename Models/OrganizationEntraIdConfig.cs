using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class OrganizationEntraIdConfig
{
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ClientSecret { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Domain { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}