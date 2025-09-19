using JoineryServer.Data;
using JoineryServer.Models;
using Microsoft.EntityFrameworkCore;

namespace JoineryServer.Services;

/// <summary>
/// Service for checking team member permissions
/// </summary>
public interface ITeamPermissionService
{
    Task<bool> HasPermissionAsync(int userId, int teamId, TeamPermission permission);
    Task<bool> HasPermissionAsync(int userId, int? teamId, TeamPermission permission);
    Task<TeamPermission> GetUserPermissionsAsync(int userId, int teamId);
    Task<TeamPermission> GetUserPermissionsAsync(int userId, int? teamId);
}

public class TeamPermissionService : ITeamPermissionService
{
    private readonly JoineryDbContext _context;

    public TeamPermissionService(JoineryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(int userId, int teamId, TeamPermission permission)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, teamId);
        return userPermissions.HasFlag(permission);
    }

    public async Task<bool> HasPermissionAsync(int userId, int? teamId, TeamPermission permission)
    {
        if (!teamId.HasValue)
            return false;

        return await HasPermissionAsync(userId, teamId.Value, permission);
    }

    public async Task<TeamPermission> GetUserPermissionsAsync(int userId, int teamId)
    {
        // Check if user is team creator (has full access)
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.IsActive);

        if (team?.CreatedByUserId == userId)
            return TeamPermission.FullAccess;

        // Get user's team membership
        var teamMember = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

        return teamMember?.GetEffectivePermissions() ?? TeamPermission.None;
    }

    public async Task<TeamPermission> GetUserPermissionsAsync(int userId, int? teamId)
    {
        if (!teamId.HasValue)
            return TeamPermission.None;

        return await GetUserPermissionsAsync(userId, teamId.Value);
    }
}