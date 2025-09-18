using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class OrganizationMember
{
    public int Id { get; set; }
    
    [Required]
    public int OrganizationId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}