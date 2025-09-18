using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoineryServer.Data;
using JoineryServer.Models;
using JoineryServer.Services;
using System.Security.Claims;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GitRepositoriesController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly IGitRepositoryService _gitService;
    private readonly ITeamPermissionService _permissionService;
    private readonly ILogger<GitRepositoriesController> _logger;

    public GitRepositoriesController(JoineryDbContext context, IGitRepositoryService gitService, ITeamPermissionService permissionService, ILogger<GitRepositoriesController> logger)
    {
        _context = context;
        _gitService = gitService;
        _permissionService = permissionService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get all Git repositories accessible to the current user
    /// </summary>
    /// <returns>List of Git repositories</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetRepositories()
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting Git repositories for user {UserId}", currentUserId);

        // Get repositories the user has access to through organizations and teams
        var repositories = await _context.GitRepositories
            .Where(r => r.IsActive && (
                // Organization-level repositories where user is a member
                (r.OrganizationId != null && r.Organization!.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive)) ||
                // Team-level repositories where user is a member  
                (r.TeamId != null && r.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive)) ||
                // Repositories created by the user
                r.CreatedByUserId == currentUserId
            ))
            .Include(r => r.Organization)
            .Include(r => r.Team)
            .Include(r => r.CreatedByUser)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.RepositoryUrl,
                r.Branch,
                r.Description,
                r.CreatedAt,
                r.UpdatedAt,
                r.LastSyncAt,
                CreatedBy = new
                {
                    r.CreatedByUser.Id,
                    r.CreatedByUser.Username,
                    r.CreatedByUser.Email
                },
                Organization = r.Organization != null ? new
                {
                    r.Organization.Id,
                    r.Organization.Name
                } : null,
                Team = r.Team != null ? new
                {
                    r.Team.Id,
                    r.Team.Name,
                    OrganizationId = r.Team.OrganizationId
                } : null,
                Scope = r.OrganizationId != null ? "Organization" : r.TeamId != null ? "Team" : "Personal"
            })
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

        return Ok(repositories);
    }

    /// <summary>
    /// Get a specific Git repository by ID
    /// </summary>
    /// <param name="id">Repository ID</param>
    /// <returns>Git repository details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetRepository(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting Git repository {RepositoryId} for user {UserId}", id, currentUserId);

        var repository = await _context.GitRepositories
            .Where(r => r.Id == id && r.IsActive)
            .Include(r => r.Organization)
            .Include(r => r.Team)
            .Include(r => r.CreatedByUser)
            .Include(r => r.QueryFiles.Where(qf => qf.IsActive))
            .FirstOrDefaultAsync();

        if (repository == null)
        {
            return NotFound();
        }

        // Check if user has access to this repository
        var hasAccess = repository.CreatedByUserId == currentUserId ||
                       (repository.OrganizationId != null && await _context.OrganizationMembers
                           .AnyAsync(om => om.OrganizationId == repository.OrganizationId && om.UserId == currentUserId && om.IsActive)) ||
                       (repository.TeamId != null && await _context.TeamMembers
                           .AnyAsync(tm => tm.TeamId == repository.TeamId && tm.UserId == currentUserId && tm.IsActive));

        if (!hasAccess)
        {
            return Forbid();
        }

        var result = new
        {
            repository.Id,
            repository.Name,
            repository.RepositoryUrl,
            repository.Branch,
            repository.Description,
            repository.CreatedAt,
            repository.UpdatedAt,
            repository.LastSyncAt,
            CreatedBy = new
            {
                repository.CreatedByUser.Id,
                repository.CreatedByUser.Username,
                repository.CreatedByUser.Email
            },
            Organization = repository.Organization != null ? new
            {
                repository.Organization.Id,
                repository.Organization.Name
            } : null,
            Team = repository.Team != null ? new
            {
                repository.Team.Id,
                repository.Team.Name,
                OrganizationId = repository.Team.OrganizationId
            } : null,
            Scope = repository.OrganizationId != null ? "Organization" : repository.TeamId != null ? "Team" : "Personal",
            QueryFiles = repository.QueryFiles.Select(qf => new
            {
                qf.Id,
                qf.FileName,
                qf.FilePath,
                qf.Description,
                qf.DatabaseType,
                qf.Tags,
                qf.LastCommitAuthor,
                qf.LastCommitAt,
                qf.LastSyncAt
            })
        };

        return Ok(result);
    }

    /// <summary>
    /// Create a new Git repository configuration
    /// </summary>
    /// <param name="request">Repository creation request</param>
    /// <returns>Created repository</returns>
    [HttpPost]
    public async Task<ActionResult<object>> CreateRepository([FromBody] CreateGitRepositoryRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} creating Git repository {Name}", currentUserId, request.Name);

        // Validate that user has permission to create repository at the specified scope
        if (request.OrganizationId.HasValue)
        {
            var hasOrgPermission = await _context.OrganizationMembers
                .AnyAsync(om => om.OrganizationId == request.OrganizationId && 
                               om.UserId == currentUserId && 
                               om.IsActive && 
                               om.Role == OrganizationRole.Administrator);
            
            if (!hasOrgPermission)
            {
                return Forbid("You must be an organization administrator to create organization-level repositories");
            }
        }

        if (request.TeamId.HasValue)
        {
            // Check if user has manage folders permission
            var hasPermission = await _permissionService.HasPermissionAsync(currentUserId, request.TeamId, TeamPermission.ManageFolders);
            
            if (!hasPermission)
            {
                return Forbid("You must have folder management permission to create team-level repositories");
            }
        }

        var repository = new GitRepository
        {
            Name = request.Name,
            RepositoryUrl = request.RepositoryUrl,
            Branch = request.Branch ?? "main",
            AccessToken = request.AccessToken,
            Description = request.Description,
            OrganizationId = request.OrganizationId,
            TeamId = request.TeamId,
            CreatedByUserId = currentUserId
        };

        _context.GitRepositories.Add(repository);
        await _context.SaveChangesAsync();

        // Sync repository immediately
        try
        {
            var queryFiles = await _gitService.SyncRepositoryAsync(repository);
            foreach (var queryFile in queryFiles)
            {
                _context.GitQueryFiles.Add(queryFile);
            }
            repository.LastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully synced {FileCount} query files from repository {RepositoryId}", 
                queryFiles.Count, repository.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync repository {RepositoryId} after creation", repository.Id);
        }

        return CreatedAtAction(nameof(GetRepository), new { id = repository.Id }, new { repository.Id, repository.Name });
    }

    /// <summary>
    /// Sync a Git repository to update query files
    /// </summary>
    /// <param name="id">Repository ID</param>
    /// <returns>Sync result</returns>
    [HttpPost("{id}/sync")]
    public async Task<ActionResult<object>> SyncRepository(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} syncing Git repository {RepositoryId}", currentUserId, id);

        var repository = await _context.GitRepositories
            .Include(r => r.QueryFiles)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (repository == null)
        {
            return NotFound();
        }

        // Check if user has access to this repository
        var hasAccess = repository.CreatedByUserId == currentUserId ||
                       (repository.OrganizationId != null && await _context.OrganizationMembers
                           .AnyAsync(om => om.OrganizationId == repository.OrganizationId && om.UserId == currentUserId && om.IsActive)) ||
                       (repository.TeamId != null && await _context.TeamMembers
                           .AnyAsync(tm => tm.TeamId == repository.TeamId && tm.UserId == currentUserId && tm.IsActive));

        if (!hasAccess)
        {
            return Forbid();
        }

        try
        {
            // Remove existing query files for this repository
            _context.GitQueryFiles.RemoveRange(repository.QueryFiles);

            // Sync new files from repository
            var queryFiles = await _gitService.SyncRepositoryAsync(repository);
            foreach (var queryFile in queryFiles)
            {
                _context.GitQueryFiles.Add(queryFile);
            }

            repository.LastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = new
            {
                RepositoryId = repository.Id,
                SyncedAt = repository.LastSyncAt,
                FileCount = queryFiles.Count,
                Files = queryFiles.Select(qf => new
                {
                    qf.FileName,
                    qf.FilePath,
                    qf.DatabaseType,
                    TagCount = qf.Tags?.Count ?? 0
                })
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync repository {RepositoryId}", id);
            return StatusCode(500, new { message = "Failed to sync repository", error = ex.Message });
        }
    }

    /// <summary>
    /// Get folders in a Git repository
    /// </summary>
    /// <param name="id">Repository ID</param>
    /// <returns>List of folder paths</returns>
    [HttpGet("{id}/folders")]
    public async Task<ActionResult<IEnumerable<string>>> GetRepositoryFolders(int id)
    {
        var currentUserId = GetCurrentUserId();
        var repository = await _context.GitRepositories.FindAsync(id);

        if (repository == null || !repository.IsActive)
        {
            return NotFound();
        }

        // Check access - at minimum need read permission
        var hasAccess = repository.CreatedByUserId == currentUserId ||
                       (repository.OrganizationId != null && await _context.OrganizationMembers
                           .AnyAsync(om => om.OrganizationId == repository.OrganizationId && om.UserId == currentUserId && om.IsActive)) ||
                       (repository.TeamId != null && await _permissionService.HasPermissionAsync(currentUserId, repository.TeamId, TeamPermission.ReadQueries));

        if (!hasAccess)
        {
            return Forbid();
        }

        try
        {
            var folders = await _gitService.GetRepositoryFoldersAsync(repository);
            return Ok(folders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get folders for repository {RepositoryId}", id);
            return StatusCode(500, new { message = "Failed to get repository folders", error = ex.Message });
        }
    }
}

public class CreateGitRepositoryRequest
{
    public string Name { get; set; } = "";
    public string RepositoryUrl { get; set; } = "";
    public string? Branch { get; set; }
    public string? AccessToken { get; set; }
    public string? Description { get; set; }
    public int? OrganizationId { get; set; }
    public int? TeamId { get; set; }
}