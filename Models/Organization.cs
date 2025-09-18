using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class Organization
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public int CreatedByUserId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<OrganizationMember> OrganizationMembers { get; set; } = new List<OrganizationMember>();
}