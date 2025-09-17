using Microsoft.AspNetCore.Mvc;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    public ActionResult<object> GetHealth()
    {
        _logger.LogInformation("Health check requested");
        
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Service = "Joinery Server"
        });
    }

    /// <summary>
    /// Readiness check endpoint
    /// </summary>
    /// <returns>Readiness status</returns>
    [HttpGet("ready")]
    public ActionResult<object> GetReadiness()
    {
        _logger.LogInformation("Readiness check requested");
        
        return Ok(new
        {
            Status = "Ready",
            Timestamp = DateTime.UtcNow,
            Message = "Service is ready to accept requests"
        });
    }
}