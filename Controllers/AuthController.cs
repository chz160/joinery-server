using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JoineryServer.Data;
using JoineryServer.Models;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JoineryDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, JoineryDbContext context, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initiate GitHub OAuth authentication
    /// </summary>
    [HttpGet("login/github")]
    public IActionResult LoginGitHub()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/callback/github"
        };

        return Challenge(properties, "GitHub");
    }

    /// <summary>
    /// Initiate Microsoft OAuth authentication
    /// </summary>
    [HttpGet("login/microsoft")]
    public IActionResult LoginMicrosoft()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/callback/microsoft"
        };

        return Challenge(properties, "MicrosoftIdentityWebApp");
    }

    /// <summary>
    /// Handle GitHub OAuth callback
    /// </summary>
    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("GitHub");

            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("GitHub authentication failed");
                return BadRequest(new { message = "GitHub authentication failed" });
            }

            var claims = authenticateResult.Principal?.Claims.ToList() ?? new List<Claim>();
            var githubId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(githubId))
            {
                return BadRequest(new { message = "Unable to get GitHub user information" });
            }

            var user = await GetOrCreateUser(githubId, username ?? "unknown", email ?? "unknown@github.com", "GitHub");
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.AuthProvider
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GitHub callback");
            return StatusCode(500, new { message = "Internal server error during authentication" });
        }
    }

    /// <summary>
    /// Handle Microsoft OAuth callback
    /// </summary>
    [HttpGet("callback/microsoft")]
    public async Task<IActionResult> MicrosoftCallback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("MicrosoftIdentityWebApp");

            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("Microsoft authentication failed");
                return BadRequest(new { message = "Microsoft authentication failed" });
            }

            var claims = authenticateResult.Principal?.Claims.ToList() ?? new List<Claim>();
            var microsoftId = claims.FirstOrDefault(c => c.Type == "oid")?.Value ??
                             claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ??
                          claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value + " " +
                          claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(microsoftId))
            {
                return BadRequest(new { message = "Unable to get Microsoft user information" });
            }

            var user = await GetOrCreateUser(microsoftId, username ?? "unknown", email ?? "unknown@microsoft.com", "Microsoft", fullName?.Trim());
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FullName,
                    user.AuthProvider
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Microsoft callback");
            return StatusCode(500, new { message = "Internal server error during authentication" });
        }
    }

    private async Task<User> GetOrCreateUser(string externalId, string username, string email, string authProvider, string? fullName = null)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.AuthProvider == authProvider);

        if (existingUser != null)
        {
            existingUser.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existingUser;
        }

        var newUser = new User
        {
            Username = username,
            Email = email,
            FullName = fullName,
            AuthProvider = authProvider,
            ExternalId = externalId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new user: {Username} via {AuthProvider}", username, authProvider);

        return newUser;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtConfig = _configuration.GetSection("JWT");
        var secretKey = jwtConfig["SecretKey"] ?? "default-secret-key";
        var issuer = jwtConfig["Issuer"] ?? "JoineryServer";
        var audience = jwtConfig["Audience"] ?? "JoineryClients";
        var expirationHours = int.Parse(jwtConfig["ExpirationHours"] ?? "24");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("auth_provider", user.AuthProvider),
            new Claim("external_id", user.ExternalId)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(expirationHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}