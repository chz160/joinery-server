using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoineryServer.Data;
using JoineryServer.Models;
using System.Security.Claims;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(JoineryDbContext context, ILogger<OrganizationsController> logger)
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
    /// Get all organizations for the current user
    /// </summary>
    /// <returns>List of organizations where user is a member or creator</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrganizations()
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting organizations for user {UserId}", currentUserId);

        var organizations = await _context.Organizations
            .Where(o => o.IsActive && (
                o.CreatedByUserId == currentUserId ||
                o.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive)
            ))
            .Include(o => o.CreatedByUser)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Description,
                o.CreatedAt,
                o.UpdatedAt,
                CreatedBy = new
                {
                    o.CreatedByUser.Id,
                    o.CreatedByUser.Username,
                    o.CreatedByUser.Email
                },
                MemberCount = o.OrganizationMembers.Count(om => om.IsActive),
                TeamCount = o.Teams.Count(t => t.IsActive),
                UserRole = o.CreatedByUserId == currentUserId ? OrganizationRole.Administrator :
                          o.OrganizationMembers.Where(om => om.UserId == currentUserId && om.IsActive)
                                               .Select(om => om.Role).FirstOrDefault()
            })
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();

        return Ok(organizations);
    }

    /// <summary>
    /// Get a specific organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details with members</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrganization(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} requesting organization {OrganizationId}", currentUserId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .Include(o => o.CreatedByUser)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
                .ThenInclude(om => om.User)
            .Include(o => o.Teams.Where(t => t.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        var isCreator = organization.CreatedByUserId == currentUserId;
        var isMember = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive);

        if (!isCreator && !isMember)
        {
            _logger.LogWarning("User {UserId} attempted to access organization {OrganizationId} without permission", currentUserId, id);
            return Forbid();
        }

        var result = new
        {
            organization.Id,
            organization.Name,
            organization.Description,
            organization.CreatedAt,
            organization.UpdatedAt,
            CreatedBy = new
            {
                organization.CreatedByUser.Id,
                organization.CreatedByUser.Username,
                organization.CreatedByUser.Email
            },
            Members = organization.OrganizationMembers.Select(om => new
            {
                om.Id,
                om.Role,
                om.JoinedAt,
                User = new
                {
                    om.User.Id,
                    om.User.Username,
                    om.User.Email,
                    om.User.FullName
                }
            }).ToList(),
            Teams = organization.Teams.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.CreatedAt,
                MemberCount = t.TeamMembers.Count(tm => tm.IsActive)
            }).ToList(),
            UserRole = isCreator ? OrganizationRole.Administrator :
                      organization.OrganizationMembers.Where(om => om.UserId == currentUserId && om.IsActive)
                                                     .Select(om => om.Role).FirstOrDefault()
        };

        return Ok(result);
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <returns>Created organization</returns>
    [HttpPost]
    public async Task<ActionResult<object>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} creating organization: {OrganizationName}", currentUserId, request.Name);

        var organization = new Organization
        {
            Name = request.Name,
            Description = request.Description,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Add creator as administrator member
        var creatorMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = currentUserId,
            Role = OrganizationRole.Administrator,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.OrganizationMembers.Add(creatorMembership);
        await _context.SaveChangesAsync();

        // Reload organization with all related data
        var createdOrganization = await _context.Organizations
            .Where(o => o.Id == organization.Id)
            .Include(o => o.CreatedByUser)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
                .ThenInclude(om => om.User)
            .FirstAsync();

        var result = new
        {
            createdOrganization.Id,
            createdOrganization.Name,
            createdOrganization.Description,
            createdOrganization.CreatedAt,
            createdOrganization.UpdatedAt,
            CreatedBy = new
            {
                createdOrganization.CreatedByUser.Id,
                createdOrganization.CreatedByUser.Username,
                createdOrganization.CreatedByUser.Email
            },
            Members = createdOrganization.OrganizationMembers.Select(om => new
            {
                om.Id,
                om.Role,
                om.JoinedAt,
                User = new
                {
                    om.User.Id,
                    om.User.Username,
                    om.User.Email,
                    om.User.FullName
                }
            }).ToList(),
            UserRole = OrganizationRole.Administrator
        };

        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, result);
    }

    /// <summary>
    /// Update an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update organization request</param>
    /// <returns>Updated organization</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating organization {OrganizationId}", currentUserId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        // Check if current user is administrator
        var isCreator = organization.CreatedByUserId == currentUserId;
        var isAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);

        if (!isCreator && !isAdmin)
        {
            return Forbid();
        }

        organization.Name = request.Name;
        organization.Description = request.Description;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetOrganization(id);
    }

    /// <summary>
    /// Delete an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrganization(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} deleting organization {OrganizationId}", currentUserId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        // Only creator can delete organization
        if (organization.CreatedByUserId != currentUserId)
        {
            return Forbid();
        }

        organization.IsActive = false;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Organization deleted successfully" });
    }

    /// <summary>
    /// Add a member to an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Add member request</param>
    /// <returns>Created organization member</returns>
    [HttpPost("{id}/members")]
    public async Task<ActionResult<object>> AddOrganizationMember(int id, [FromBody] AddOrganizationMemberRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} adding member {MemberId} to organization {OrganizationId}", currentUserId, request.UserId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        // Check if current user is administrator
        var isCreator = organization.CreatedByUserId == currentUserId;
        var isAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);

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
        var existingMembership = organization.OrganizationMembers.FirstOrDefault(om => om.UserId == request.UserId);
        if (existingMembership != null)
        {
            if (existingMembership.IsActive)
            {
                return BadRequest("User is already a member of this organization");
            }
            else
            {
                // Reactivate existing membership
                existingMembership.IsActive = true;
                existingMembership.Role = request.Role;
                existingMembership.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Create new membership
            var newMembership = new OrganizationMember
            {
                OrganizationId = id,
                UserId = request.UserId,
                Role = request.Role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.OrganizationMembers.Add(newMembership);
            await _context.SaveChangesAsync();
            existingMembership = newMembership;
        }

        // Load updated membership with user details
        var updatedMember = await _context.OrganizationMembers
            .Where(om => om.Id == existingMembership.Id)
            .Include(om => om.User)
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

    /// <summary>
    /// Remove a member from an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult> RemoveOrganizationMember(int id, int userId)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} removing member {MemberId} from organization {OrganizationId}", currentUserId, userId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        var membershipToRemove = organization.OrganizationMembers.FirstOrDefault(om => om.UserId == userId && om.IsActive);
        if (membershipToRemove == null)
        {
            return NotFound("Organization member not found");
        }

        // Check permissions: admin can remove anyone, users can remove themselves, creator cannot be removed
        var isCreator = organization.CreatedByUserId == currentUserId;
        var isAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);
        var isSelf = userId == currentUserId;

        if (!isCreator && !isAdmin && !isSelf)
        {
            return Forbid();
        }

        // Prevent removing the organization creator
        if (organization.CreatedByUserId == userId)
        {
            return BadRequest("Cannot remove organization creator");
        }

        membershipToRemove.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Member removed successfully" });
    }

    /// <summary>
    /// Update member role in an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update role request</param>
    /// <returns>Updated member</returns>
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<ActionResult<object>> UpdateMemberRole(int id, int userId, [FromBody] UpdateOrganizationMemberRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating role for member {MemberId} in organization {OrganizationId}", currentUserId, userId, id);

        var organization = await _context.Organizations
            .Where(o => o.Id == id && o.IsActive)
            .Include(o => o.OrganizationMembers.Where(om => om.IsActive))
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        var membership = organization.OrganizationMembers.FirstOrDefault(om => om.UserId == userId && om.IsActive);
        if (membership == null)
        {
            return NotFound("Organization member not found");
        }

        // Check if current user is administrator
        var isCreator = organization.CreatedByUserId == currentUserId;
        var isAdmin = organization.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive && om.Role == OrganizationRole.Administrator);

        if (!isCreator && !isAdmin)
        {
            return Forbid();
        }

        // Prevent demoting the organization creator
        if (organization.CreatedByUserId == userId && request.Role != OrganizationRole.Administrator)
        {
            return BadRequest("Cannot change role of organization creator");
        }

        membership.Role = request.Role;
        await _context.SaveChangesAsync();

        // Load updated membership with user details
        var updatedMember = await _context.OrganizationMembers
            .Where(om => om.Id == membership.Id)
            .Include(om => om.User)
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
public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AddOrganizationMemberRequest
{
    public int UserId { get; set; }
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
}

public class UpdateOrganizationMemberRoleRequest
{
    public OrganizationRole Role { get; set; }
}