using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoineryServer.Data;
using JoineryServer.Models;
using JoineryServer.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueriesController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly IGitRepositoryService _gitService;
    private readonly ITeamPermissionService _permissionService;
    private readonly ILogger<QueriesController> _logger;

    public QueriesController(JoineryDbContext context, IGitRepositoryService gitService, ITeamPermissionService permissionService, ILogger<QueriesController> logger)
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
    /// Get all database queries (both traditional and Git-based)
    /// </summary>
    /// <returns>List of database queries</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetQueries()
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting all database queries for user {UserId}", currentUserId);

        // Get traditional database queries
        var dbQueries = await _context.DatabaseQueries
            .Where(q => q.IsActive)
            .Select(q => new
            {
                q.Id,
                q.Name,
                SqlQuery = q.SqlQuery,
                q.Description,
                q.CreatedBy,
                q.CreatedAt,
                q.UpdatedAt,
                q.DatabaseType,
                q.Tags,
                Source = "Database",
                RepositoryName = (string?)null,
                FilePath = (string?)null,
                LastCommitAuthor = (string?)null,
                LastCommitAt = (DateTime?)null
            })
            .ToListAsync();

        // Get Git-based queries from accessible repositories
        var gitQueries = await _context.GitQueryFiles
            .Where(gqf => gqf.IsActive && gqf.GitRepository.IsActive && (
                // Organization-level repositories where user is a member
                (gqf.GitRepository.OrganizationId != null && gqf.GitRepository.Organization!.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive)) ||
                // Team-level repositories where user is a member  
                (gqf.GitRepository.TeamId != null && gqf.GitRepository.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive)) ||
                // Repositories created by the user
                gqf.GitRepository.CreatedByUserId == currentUserId
            ))
            .Include(gqf => gqf.GitRepository)
            .Select(gqf => new
            {
                Id = gqf.Id + 10000, // Offset to avoid ID conflicts with database queries
                Name = gqf.FileName.Replace(".sql", "").Replace("_", " "),
                SqlQuery = gqf.SqlContent,
                Description = gqf.Description ?? $"Query from {gqf.GitRepository.Name}",
                CreatedBy = gqf.LastCommitAuthor ?? "Unknown",
                CreatedAt = gqf.LastCommitAt,
                UpdatedAt = gqf.LastSyncAt,
                DatabaseType = gqf.DatabaseType,
                Tags = gqf.Tags,
                Source = "Git",
                RepositoryName = gqf.GitRepository.Name,
                FilePath = gqf.FilePath,
                LastCommitAuthor = gqf.LastCommitAuthor,
                LastCommitAt = gqf.LastCommitAt
            })
            .ToListAsync();

        // Combine both sources
        var allQueries = dbQueries.Cast<object>().Concat(gitQueries.Cast<object>())
            .OrderByDescending(q => ((dynamic)q).UpdatedAt)
            .ToList();

        return Ok(allQueries);
    }

    /// <summary>
    /// Get a specific database query by ID
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <returns>Database query</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetQuery(int id)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting database query with ID: {Id} for user {UserId}", id, currentUserId);

        // Check if it's a Git-based query (ID > 10000)
        if (id > 10000)
        {
            var gitQueryId = id - 10000;
            var gitQuery = await _context.GitQueryFiles
                .Where(gqf => gqf.Id == gitQueryId && gqf.IsActive && gqf.GitRepository.IsActive && (
                    // Organization-level repositories where user is a member
                    (gqf.GitRepository.OrganizationId != null && gqf.GitRepository.Organization!.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive)) ||
                    // Team-level repositories where user is a member  
                    (gqf.GitRepository.TeamId != null && gqf.GitRepository.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive)) ||
                    // Repositories created by the user
                    gqf.GitRepository.CreatedByUserId == currentUserId
                ))
                .Include(gqf => gqf.GitRepository)
                .Select(gqf => new
                {
                    Id = gqf.Id + 10000,
                    Name = gqf.FileName.Replace(".sql", "").Replace("_", " "),
                    SqlQuery = gqf.SqlContent,
                    Description = gqf.Description ?? $"Query from {gqf.GitRepository.Name}",
                    CreatedBy = gqf.LastCommitAuthor ?? "Unknown",
                    CreatedAt = gqf.LastCommitAt,
                    UpdatedAt = gqf.LastSyncAt,
                    DatabaseType = gqf.DatabaseType,
                    Tags = gqf.Tags,
                    Source = "Git",
                    RepositoryName = gqf.GitRepository.Name,
                    FilePath = gqf.FilePath,
                    LastCommitAuthor = gqf.LastCommitAuthor,
                    LastCommitAt = gqf.LastCommitAt
                })
                .FirstOrDefaultAsync();

            if (gitQuery == null)
            {
                _logger.LogWarning("Git-based database query with ID {Id} not found", id);
                return NotFound();
            }

            return Ok(gitQuery);
        }

        // Traditional database query
        var query = await _context.DatabaseQueries
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (query == null)
        {
            _logger.LogWarning("Database query with ID {Id} not found", id);
            return NotFound();
        }

        var result = new
        {
            query.Id,
            query.Name,
            SqlQuery = query.SqlQuery,
            query.Description,
            query.CreatedBy,
            query.CreatedAt,
            query.UpdatedAt,
            query.DatabaseType,
            query.Tags,
            Source = "Database",
            RepositoryName = (string?)null,
            FilePath = (string?)null,
            LastCommitAuthor = (string?)null,
            LastCommitAt = (DateTime?)null
        };

        return Ok(result);
    }

    /// <summary>
    /// Search queries by name or tags (both traditional and Git-based)
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Filtered list of database queries</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<object>>> SearchQueries([FromQuery] string searchTerm)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Searching database queries with term: {SearchTerm} for user {UserId}", searchTerm, currentUserId);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term cannot be empty");
        }

        // Search traditional database queries
        var dbQueries = await _context.DatabaseQueries
            .Where(q => q.IsActive &&
                       (q.Name.Contains(searchTerm) ||
                        q.Description != null && q.Description.Contains(searchTerm) ||
                        q.Tags != null && q.Tags.Any(t => t.Contains(searchTerm))))
            .Select(q => new
            {
                Id = q.Id,
                Name = q.Name,
                SqlQuery = q.SqlQuery,
                Description = q.Description,
                CreatedBy = q.CreatedBy,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                DatabaseType = q.DatabaseType,
                Tags = q.Tags,
                Source = "Database",
                RepositoryName = (string?)null,
                FilePath = (string?)null
            })
            .ToListAsync();

        // Search Git-based queries
        var gitQueries = await _context.GitQueryFiles
            .Where(gqf => gqf.IsActive && gqf.GitRepository.IsActive &&
                         (gqf.FileName.Contains(searchTerm) ||
                          gqf.Description != null && gqf.Description.Contains(searchTerm) ||
                          gqf.Tags != null && gqf.Tags.Any(t => t.Contains(searchTerm)) ||
                          gqf.SqlContent != null && gqf.SqlContent.Contains(searchTerm)) &&
                         (
                             // Organization-level repositories where user is a member
                             (gqf.GitRepository.OrganizationId != null && gqf.GitRepository.Organization!.OrganizationMembers.Any(om => om.UserId == currentUserId && om.IsActive)) ||
                             // Team-level repositories where user is a member  
                             (gqf.GitRepository.TeamId != null && gqf.GitRepository.Team!.TeamMembers.Any(tm => tm.UserId == currentUserId && tm.IsActive)) ||
                             // Repositories created by the user
                             gqf.GitRepository.CreatedByUserId == currentUserId
                         ))
            .Include(gqf => gqf.GitRepository)
            .Select(gqf => new
            {
                Id = gqf.Id + 10000,
                Name = gqf.FileName.Replace(".sql", "").Replace("_", " "),
                SqlQuery = gqf.SqlContent,
                Description = gqf.Description ?? $"Query from {gqf.GitRepository.Name}",
                CreatedBy = gqf.LastCommitAuthor ?? "Unknown",
                CreatedAt = gqf.LastCommitAt,
                UpdatedAt = gqf.LastSyncAt,
                DatabaseType = gqf.DatabaseType,
                Tags = gqf.Tags,
                Source = "Git",
                RepositoryName = gqf.GitRepository.Name,
                FilePath = gqf.FilePath
            })
            .ToListAsync();

        // Combine both sources
        var allQueries = dbQueries.Cast<object>().Concat(gitQueries.Cast<object>())
            .OrderByDescending(q => ((dynamic)q).UpdatedAt)
            .ToList();

        return Ok(allQueries);
    }

    /// <summary>
    /// Get queries by database type
    /// </summary>
    /// <param name="databaseType">Database type (e.g., PostgreSQL, MySQL, SQLServer)</param>
    /// <returns>Filtered list of database queries</returns>
    [HttpGet("by-database/{databaseType}")]
    public async Task<ActionResult<IEnumerable<DatabaseQuery>>> GetQueriesByDatabase(string databaseType)
    {
        _logger.LogInformation("Getting database queries for database type: {DatabaseType}", databaseType);

        var queries = await _context.DatabaseQueries
            .Where(q => q.IsActive && q.DatabaseType == databaseType)
            .OrderByDescending(q => q.UpdatedAt)
            .ToListAsync();

        return Ok(queries);
    }

    /// <summary>
    /// Get queries by tags
    /// </summary>
    /// <param name="tag">Tag to filter by</param>
    /// <returns>Filtered list of database queries</returns>
    [HttpGet("by-tag/{tag}")]
    public async Task<ActionResult<IEnumerable<DatabaseQuery>>> GetQueriesByTag(string tag)
    {
        _logger.LogInformation("Getting database queries with tag: {Tag}", tag);

        var queries = await _context.DatabaseQueries
            .Where(q => q.IsActive && q.Tags != null && q.Tags.Contains(tag))
            .OrderByDescending(q => q.UpdatedAt)
            .ToListAsync();

        return Ok(queries);
    }

    /// <summary>
    /// Get queries from a specific Git repository folder
    /// </summary>
    /// <param name="repositoryId">Git repository ID</param>
    /// <param name="folderPath">Folder path (optional, defaults to root)</param>
    /// <returns>List of queries in the specified folder</returns>
    [HttpGet("from-git/{repositoryId}/folder")]
    public async Task<ActionResult<IEnumerable<object>>> GetQueriesFromGitFolder(int repositoryId, [FromQuery] string folderPath = "")
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting queries from Git repository {RepositoryId} folder '{FolderPath}' for user {UserId}",
            repositoryId, folderPath, currentUserId);

        var repository = await _context.GitRepositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId && r.IsActive);

        if (repository == null)
        {
            return NotFound("Repository not found");
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

        IQueryable<GitQueryFile> query = _context.GitQueryFiles
            .Where(gqf => gqf.GitRepositoryId == repositoryId && gqf.IsActive)
            .Include(gqf => gqf.GitRepository);

        // Filter by folder path
        if (!string.IsNullOrEmpty(folderPath))
        {
            var normalizedFolderPath = folderPath.Replace("\\", "/").TrimEnd('/');
            query = query.Where(gqf => gqf.FilePath.StartsWith(normalizedFolderPath + "/"));
        }
        else
        {
            // Root folder only (no subfolders)
            query = query.Where(gqf => !gqf.FilePath.Contains("/"));
        }

        var gitQueries = await query
            .Select(gqf => new
            {
                Id = gqf.Id + 10000,
                Name = gqf.FileName.Replace(".sql", "").Replace("_", " "),
                SqlQuery = gqf.SqlContent,
                Description = gqf.Description ?? $"Query from {gqf.GitRepository.Name}",
                CreatedBy = gqf.LastCommitAuthor ?? "Unknown",
                CreatedAt = gqf.LastCommitAt,
                UpdatedAt = gqf.LastSyncAt,
                DatabaseType = gqf.DatabaseType,
                Tags = gqf.Tags,
                Source = "Git",
                RepositoryName = gqf.GitRepository.Name,
                FilePath = gqf.FilePath,
                LastCommitAuthor = gqf.LastCommitAuthor,
                LastCommitAt = gqf.LastCommitAt
            })
            .OrderBy(gq => gq.FilePath)
            .ToListAsync();

        return Ok(gitQueries);
    }

    /// <summary>
    /// Get file history for a Git-based query
    /// </summary>
    /// <param name="repositoryId">Git repository ID</param>
    /// <param name="filePath">File path within the repository</param>
    /// <returns>Basic change history information</returns>
    [HttpGet("from-git/{repositoryId}/history")]
    public async Task<ActionResult<object>> GetQueryFileHistory(int repositoryId, [FromQuery] string filePath)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("Getting history for file '{FilePath}' in repository {RepositoryId} for user {UserId}",
            filePath, repositoryId, currentUserId);

        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest("File path is required");
        }

        var repository = await _context.GitRepositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId && r.IsActive);

        if (repository == null)
        {
            return NotFound("Repository not found");
        }

        // Check access
        var hasAccess = repository.CreatedByUserId == currentUserId ||
                       (repository.OrganizationId != null && await _context.OrganizationMembers
                           .AnyAsync(om => om.OrganizationId == repository.OrganizationId && om.UserId == currentUserId && om.IsActive)) ||
                       (repository.TeamId != null && await _context.TeamMembers
                           .AnyAsync(tm => tm.TeamId == repository.TeamId && tm.UserId == currentUserId && tm.IsActive));

        if (!hasAccess)
        {
            return Forbid();
        }

        var queryFile = await _context.GitQueryFiles
            .Include(gqf => gqf.GitRepository)
            .FirstOrDefaultAsync(gqf => gqf.GitRepositoryId == repositoryId && gqf.FilePath == filePath && gqf.IsActive);

        if (queryFile == null)
        {
            return NotFound("Query file not found");
        }

        var history = new
        {
            RepositoryName = queryFile.GitRepository.Name,
            RepositoryUrl = queryFile.GitRepository.RepositoryUrl,
            FilePath = queryFile.FilePath,
            FileName = queryFile.FileName,
            LastCommit = new
            {
                Sha = queryFile.LastCommitSha,
                Author = queryFile.LastCommitAuthor,
                Date = queryFile.LastCommitAt,
                Message = "Latest commit" // We could enhance this to get actual commit messages
            },
            LastSyncAt = queryFile.LastSyncAt,
            // Note: For full commit history, we'd need to make additional API calls to the Git provider
            Note = "For complete commit history, visit the repository directly"
        };

        return Ok(history);
    }

    /// <summary>
    /// Create a new database query (requires create permission for team queries)
    /// </summary>
    /// <param name="request">Query creation request</param>
    /// <returns>Created query</returns>
    [HttpPost]
    public async Task<ActionResult<object>> CreateQuery([FromBody] CreateQueryRequest request)
    {
        var currentUserId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} creating query {Name}", currentUserId, request.Name);

        // If associated with a team, check permissions
        if (request.TeamId.HasValue)
        {
            var hasPermission = await _permissionService.HasPermissionAsync(currentUserId, request.TeamId, TeamPermission.CreateQueries);
            if (!hasPermission)
            {
                return Forbid("You don't have permission to create queries in this team");
            }
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
        {
            return BadRequest("User not found");
        }

        var query = new DatabaseQuery
        {
            Name = request.Name,
            SqlQuery = request.SqlQuery,
            Description = request.Description,
            CreatedBy = currentUser.Username,
            DatabaseType = request.DatabaseType,
            Tags = request.Tags,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.DatabaseQueries.Add(query);
        await _context.SaveChangesAsync();

        var result = new
        {
            query.Id,
            query.Name,
            query.SqlQuery,
            query.Description,
            query.CreatedBy,
            query.CreatedAt,
            query.UpdatedAt,
            query.DatabaseType,
            query.Tags
        };

        return CreatedAtAction(nameof(GetQuery), new { id = query.Id }, result);
    }
}

/// <summary>
/// Request DTO for creating queries
/// </summary>
public class CreateQueryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SqlQuery { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? DatabaseType { get; set; }

    public List<string>? Tags { get; set; }

    /// <summary>
    /// Optional team ID if this query is associated with a team
    /// </summary>
    public int? TeamId { get; set; }
}