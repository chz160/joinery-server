using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoineryServer.Data;
using JoineryServer.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(JoineryDbContext context, ILogger<TeamsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Get all teams for the current user
    /// </summary>
    /// <returns>List of teams where user is a member or creator</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTeams()
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting teams for user {UserId}", currentUserId);

        var teams = await _context.Teams
            .Where(t => t.IsActive && (
                t.CreatedByUserId == currentUserId || 
                t.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive)
            ))
            .Include(t => t.CreatedByUser)
            .Include(t => t.Organization)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .ThenInclude(tm => tm.User)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.CreatedAt,
                t.UpdatedAt,
                CreatedBy = new
                {
                    t.CreatedByUser.Id,
                    t.CreatedByUser.Username,
                    t.CreatedByUser.Email
                },
                Organization = new
                {
                    t.Organization.Id,
                    t.Organization.Name
                },
                MemberCount = t.TeamMembers.Count(tm => tm.IsActive),
                UserRole = t.CreatedByUserId == currentUserId ? TeamRole.Administrator :
                          t.TeamMembers.Where(tm => tm.UserId == currentUserId && tm.IsActive)
                                       .Select(tm => tm.Role).FirstOrDefault()
            })
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        return Ok(teams);
    }

    /// <summary>
    /// Get a specific team by ID
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>Team details with members</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTeam(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting team {TeamId} for user {UserId}", id, currentUserId);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Organization)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync();

        if (team == null)
        {
            _logger.LogWarning("Team {TeamId} not found", id);
            return NotFound();
        }

        // Check if user has access to this team
        var isCreator = team.CreatedByUserId == currentUserId;
        var isMember = team.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive);
        
        if (!isCreator && !isMember)
        {
            _logger.LogWarning("User {UserId} attempted to access team {TeamId} without permission", currentUserId, id);
            return Forbid();
        }

        var result = new
        {
            team.Id,
            team.Name,
            team.Description,
            team.CreatedAt,
            team.UpdatedAt,
            CreatedBy = new
            {
                team.CreatedByUser.Id,
                team.CreatedByUser.Username,
                team.CreatedByUser.Email
            },
            Organization = new
            {
                team.Organization.Id,
                team.Organization.Name
            },
            Members = team.TeamMembers.Select(tm => new
            {
                tm.Id,
                tm.Role,
                tm.JoinedAt,
                User = new
                {
                    tm.User.Id,
                    tm.User.Username,
                    tm.User.Email,
                    tm.User.FullName
                }
            }).ToList(),
            UserRole = isCreator ? TeamRole.Administrator :
                      team.TeamMembers.Where(tm => tm.UserId == currentUserId && tm.IsActive)
                                      .Select(tm => tm.Role).FirstOrDefault()
        };

        return Ok(result);
    }

    /// <summary>
    /// Create a new team
    /// </summary>
    /// <param name="request">Team creation request</param>
    /// <returns>Created team</returns>
    [HttpPost]
    public async Task<ActionResult<object>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} creating team: {TeamName}", currentUserId, request.Name);

        // Validate that user has access to the organization (now required)
        var organization = await _context.Organizations
            .Where(o => o.Id == request.OrganizationId && o.IsActive)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return BadRequest("Organization not found");
        }

        var isOrgCreator = organization.CreatedByUserId == currentUserId;
        var isOrgAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);

        if (!isOrgCreator && !isOrgAdmin)
        {
            return Forbid("You must be an administrator of the organization to create teams within it");
        }

        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            OrganizationId = request.OrganizationId,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        // Add creator as administrator member
        var creatorMembership = new TeamMember
        {
            TeamId = team.Id,
            UserId = currentUserId,
            Role = TeamRole.Administrator,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.TeamMembers.Add(creatorMembership);
        await _context.SaveChangesAsync();

        // Load the created team with relationships
        var createdTeam = await _context.Teams
            .Where(t => t.Id == team.Id)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Organization)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .ThenInclude(tm => tm.User)
            .FirstAsync();

        var result = new
        {
            createdTeam.Id,
            createdTeam.Name,
            createdTeam.Description,
            createdTeam.CreatedAt,
            createdTeam.UpdatedAt,
            CreatedBy = new
            {
                createdTeam.CreatedByUser.Id,
                createdTeam.CreatedByUser.Username,
                createdTeam.CreatedByUser.Email
            },
            Organization = new
            {
                createdTeam.Organization.Id,
                createdTeam.Organization.Name
            },
            Members = createdTeam.TeamMembers.Select(tm => new
            {
                tm.Id,
                tm.Role,
                tm.JoinedAt,
                User = new
                {
                    tm.User.Id,
                    tm.User.Username,
                    tm.User.Email,
                    tm.User.FullName
                }
            }).ToList(),
            UserRole = TeamRole.Administrator
        };

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, result);
    }

    /// <summary>
    /// Update a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="request">Team update request</param>
    /// <returns>Updated team</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateTeam(int id, [FromBody] UpdateTeamRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating team {TeamId}", currentUserId, id);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound();
        }

        // Check if user is administrator
        var isCreator = team.CreatedByUserId == currentUserId;
        var isAdmin = team.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive && tm.Role == TeamRole.Administrator);
        
        if (!isCreator && !isAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to update team {TeamId} without admin permission", currentUserId, id);
            return Forbid();
        }

        // Validate access to the organization (now required)
        if (request.OrganizationId != team.OrganizationId)
        {
            var organization = await _context.Organizations
                .Where(o => o.Id == request.OrganizationId && o.IsActive)
                .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
                .FirstOrDefaultAsync();

            if (organization == null)
            {
                return BadRequest("Organization not found");
            }

            var isOrgCreator = organization.CreatedByUserId == currentUserId;
            var isOrgAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);

            if (!isOrgCreator && !isOrgAdmin)
            {
                return Forbid("You must be an administrator of the organization to move teams into it");
            }
        }

        team.Name = request.Name;
        team.Description = request.Description;
        team.OrganizationId = request.OrganizationId;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetTeam(id);
    }

    /// <summary>
    /// Delete a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTeam(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} deleting team {TeamId}", currentUserId, id);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound();
        }

        // Only creator can delete team
        if (team.CreatedByUserId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to delete team {TeamId} without creator permission", currentUserId, id);
            return Forbid();
        }

        team.IsActive = false;
        team.UpdatedAt = DateTime.UtcNow;
        
        // Also deactivate all team memberships
        var memberships = await _context.TeamMembers
            .Where(tm => tm.TeamId == id && tm.IsActive)
            .ToListAsync();
        
        foreach (var membership in memberships)
        {
            membership.IsActive = false;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Add a member to a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="request">Add member request</param>
    /// <returns>Created team member</returns>
    [HttpPost("{id}/members")]
    public async Task<ActionResult<object>> AddTeamMember(int id, [FromBody] AddTeamMemberRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} adding member {MemberId} to team {TeamId}", currentUserId, request.UserId, id);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound("Team not found");
        }

        // Check if current user is administrator
        var isCreator = team.CreatedByUserId == currentUserId;
        var isAdmin = team.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive && tm.Role == TeamRole.Administrator);
        
        if (!isCreator && !isAdmin)
        {
            return Forbid();
        }

        // Check if user to add exists
        var userToAdd = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive);
        if (userToAdd == null)
        {
            return BadRequest("User not found");
        }

        // Check if user is already a member
        var existingMembership = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == request.UserId && tm.IsActive);
        
        if (existingMembership != null)
        {
            return BadRequest("User is already a member of this team");
        }

        var teamMember = new TeamMember
        {
            TeamId = id,
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.TeamMembers.Add(teamMember);
        await _context.SaveChangesAsync();

        // Load the created team member with user details
        var createdMember = await _context.TeamMembers
            .Where(tm => tm.Id == teamMember.Id)
            .Include(tm => tm.User)
            .FirstAsync();

        var result = new
        {
            createdMember.Id,
            createdMember.Role,
            createdMember.JoinedAt,
            User = new
            {
                createdMember.User.Id,
                createdMember.User.Username,
                createdMember.User.Email,
                createdMember.User.FullName
            }
        };

        return CreatedAtAction(nameof(GetTeam), new { id = id }, result);
    }

    /// <summary>
    /// Remove a member from a team
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult> RemoveTeamMember(int id, int userId)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} removing member {MemberId} from team {TeamId}", currentUserId, userId, id);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound("Team not found");
        }

        var membership = team.TeamMembers.FirstOrDefault(tm => tm.UserId == userId && tm.IsActive);
        if (membership == null)
        {
            return NotFound("Team member not found");
        }

        // Check permissions: admin can remove anyone, user can remove themselves
        var isCreator = team.CreatedByUserId == currentUserId;
        var isAdmin = team.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive && tm.Role == TeamRole.Administrator);
        var isSelf = currentUserId == userId;
        
        if (!isCreator && !isAdmin && !isSelf)
        {
            return Forbid();
        }

        // Prevent removing the team creator
        if (team.CreatedByUserId == userId && currentUserId != userId)
        {
            return BadRequest("Cannot remove team creator");
        }

        membership.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Update a team member's role
    /// </summary>
    /// <param name="id">Team ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated team member</returns>
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<ActionResult<object>> UpdateMemberRole(int id, int userId, [FromBody] UpdateMemberRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating role for member {MemberId} in team {TeamId}", currentUserId, userId, id);

        var team = await _context.Teams
            .Where(t => t.Id == id && t.IsActive)
            .Include(t => t.TeamMembers.Where(tm => tm.IsActive))
            .FirstOrDefaultAsync();

        if (team == null)
        {
            return NotFound("Team not found");
        }

        var membership = team.TeamMembers.FirstOrDefault(tm => tm.UserId == userId && tm.IsActive);
        if (membership == null)
        {
            return NotFound("Team member not found");
        }

        // Check if current user is administrator
        var isCreator = team.CreatedByUserId == currentUserId;
        var isAdmin = team.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive && tm.Role == TeamRole.Administrator);
        
        if (!isCreator && !isAdmin)
        {
            return Forbid();
        }

        // Prevent demoting the team creator
        if (team.CreatedByUserId == userId && request.Role != TeamRole.Administrator)
        {
            return BadRequest("Cannot change role of team creator");
        }

        membership.Role = request.Role;
        await _context.SaveChangesAsync();

        // Load updated membership with user details
        var updatedMember = await _context.TeamMembers
            .Where(tm => tm.Id == membership.Id)
            .Include(tm => tm.User)
            .FirstAsync();

        var result = new
        {
            updatedMember.Id,
            updatedMember.Role,
            updatedMember.JoinedAt,
            User = new
            {
                updatedMember.User.Id,
                updatedMember.User.Username,
                updatedMember.User.Email,
                updatedMember.User.FullName
            }
        };

        return Ok(result);
    }
}

// Request DTOs
public class CreateTeamRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public int OrganizationId { get; set; }
}

public class UpdateTeamRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public int OrganizationId { get; set; }
}

public class AddTeamMemberRequest
{
    public int UserId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;
}

public class UpdateMemberRoleRequest
{
    public TeamRole Role { get; set; }
}