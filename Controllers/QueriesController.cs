using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoineryServer.Data;
using JoineryServer.Models;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueriesController : ControllerBase
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<QueriesController> _logger;

    public QueriesController(JoineryDbContext context, ILogger<QueriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all database queries
    /// </summary>
    /// <returns>List of database queries</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DatabaseQuery>>> GetQueries()
    {
        _logger.LogInformation("Getting all database queries");
        
        var queries = await _context.DatabaseQueries
            .Where(q => q.IsActive)
            .OrderByDescending(q => q.UpdatedAt)
            .ToListAsync();

        return Ok(queries);
    }

    /// <summary>
    /// Get a specific database query by ID
    /// </summary>
    /// <param name="id">Query ID</param>
    /// <returns>Database query</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<DatabaseQuery>> GetQuery(int id)
    {
        _logger.LogInformation("Getting database query with ID: {Id}", id);

        var query = await _context.DatabaseQueries
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (query == null)
        {
            _logger.LogWarning("Database query with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(query);
    }

    /// <summary>
    /// Search queries by name or tags
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Filtered list of database queries</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DatabaseQuery>>> SearchQueries([FromQuery] string searchTerm)
    {
        _logger.LogInformation("Searching database queries with term: {SearchTerm}", searchTerm);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term cannot be empty");
        }

        var queries = await _context.DatabaseQueries
            .Where(q => q.IsActive && 
                       (q.Name.Contains(searchTerm) || 
                        q.Description != null && q.Description.Contains(searchTerm) ||
                        q.Tags != null && q.Tags.Any(t => t.Contains(searchTerm))))
            .OrderByDescending(q => q.UpdatedAt)
            .ToListAsync();

        return Ok(queries);
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
}