using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class TeamMember
{
    public int Id { get; set; }
    
    [Required]
    public int TeamId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public TeamRole Role { get; set; } = TeamRole.Member;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}