using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class Team
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    public int OrganizationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}