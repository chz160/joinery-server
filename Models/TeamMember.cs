namespace JoineryServer.Models;

public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public int UserId { get; set; }

    public TeamRole Role { get; set; } = TeamRole.Member;

    /// <summary>
    /// Granular permissions for this team member
    /// If null, defaults to permissions based on Role for backwards compatibility
    /// </summary>
    public TeamPermission? Permissions { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets the effective permissions for this team member
    /// </summary>
    public TeamPermission GetEffectivePermissions()
    {
        // If specific permissions are set, use those
        if (Permissions.HasValue)
            return Permissions.Value;

        // Otherwise, default based on role for backwards compatibility
        return Role switch
        {
            TeamRole.Administrator => TeamPermission.FullAccess,
            TeamRole.Member => TeamPermissionLevels.ReadOnly,
            _ => TeamPermission.None
        };
    }
}