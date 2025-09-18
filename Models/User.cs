using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? FullName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AuthProvider { get; set; } = string.Empty; // "GitHub" or "Microsoft"
    
    [Required]
    [MaxLength(100)]
    public string ExternalId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Team> CreatedTeams { get; set; } = new List<Team>();
    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
}