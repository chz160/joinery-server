namespace JoineryServer.Models;

public class OrganizationMember
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int UserId { get; set; }

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}