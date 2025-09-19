using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JoineryServer.Data;
using JoineryServer.Models;
using JoineryServer.Services;

namespace JoineryServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JoineryDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IAwsIamService _awsIamService;
    private readonly IEntraIdService _entraIdService;

    public AuthController(IConfiguration configuration, JoineryDbContext context, ILogger<AuthController> logger, IAwsIamService awsIamService, IEntraIdService entraIdService)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _awsIamService = awsIamService;
        _entraIdService = entraIdService;
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

    /// <summary>
    /// Authenticate with AWS IAM credentials
    /// </summary>
    [HttpPost("login/aws")]
    public async Task<IActionResult> LoginAws([FromBody] AwsLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.OrganizationName))
            {
                return BadRequest(new { message = "Username and organization name are required" });
            }

            // Find the organization and its AWS IAM configuration
            var organization = await _context.Organizations
                .Include(o => o.OrganizationMembers)
                .FirstOrDefaultAsync(o => o.Name == request.OrganizationName && o.IsActive);

            if (organization == null)
            {
                return BadRequest(new { message = "Organization not found" });
            }

            var awsConfig = await _context.OrganizationAwsIamConfigs
                .FirstOrDefaultAsync(c => c.OrganizationId == organization.Id && c.IsActive);

            if (awsConfig == null)
            {
                return BadRequest(new { message = "AWS IAM not configured for this organization" });
            }

            // Get the IAM user from AWS
            var awsUser = await _awsIamService.GetIamUserAsync(awsConfig, request.Username);
            if (awsUser == null)
            {
                return BadRequest(new { message = "User not found in AWS IAM" });
            }

            // Check if user exists in our system or create them
            var user = await GetOrCreateUser(awsUser.UserId, awsUser.Username, awsUser.Email, "AWS", awsUser.FullName);

            // Verify the user is a member of this organization
            var orgMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == organization.Id && m.UserId == user.Id);

            if (orgMember == null)
            {
                return BadRequest(new { message = "User is not a member of this organization" });
            }

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
            _logger.LogError(ex, "Error during AWS IAM authentication");
            return StatusCode(500, new { message = "Internal server error during authentication" });
        }
    }

    /// <summary>
    /// Authenticate with Entra ID credentials for a specific organization
    /// </summary>
    [HttpPost("login/entra-id")]
    public async Task<IActionResult> LoginEntraId([FromBody] EntraIdLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserPrincipalName) || string.IsNullOrEmpty(request.OrganizationName))
            {
                return BadRequest(new { message = "User principal name and organization name are required" });
            }

            // Find the organization and its Entra ID configuration
            var organization = await _context.Organizations
                .Include(o => o.OrganizationMembers)
                .FirstOrDefaultAsync(o => o.Name == request.OrganizationName && o.IsActive);

            if (organization == null)
            {
                return BadRequest(new { message = "Organization not found" });
            }

            var entraIdConfig = await _context.OrganizationEntraIdConfigs
                .FirstOrDefaultAsync(c => c.OrganizationId == organization.Id && c.IsActive);

            if (entraIdConfig == null)
            {
                return BadRequest(new { message = "Entra ID not configured for this organization" });
            }

            // Get the user from Entra ID
            var entraIdUser = await _entraIdService.GetEntraIdUserAsync(entraIdConfig, request.UserPrincipalName);
            if (entraIdUser == null)
            {
                return BadRequest(new { message = "User not found in Entra ID or not part of the configured domain" });
            }

            // Check if user exists in our system or create them
            var fullName = $"{entraIdUser.GivenName} {entraIdUser.Surname}".Trim();
            if (string.IsNullOrEmpty(fullName))
            {
                fullName = entraIdUser.DisplayName;
            }

            var user = await GetOrCreateUser(entraIdUser.UserId, entraIdUser.UserPrincipalName, entraIdUser.Email, "Microsoft", fullName);

            // Verify the user is a member of this organization
            var orgMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == organization.Id && m.UserId == user.Id);

            if (orgMember == null)
            {
                return BadRequest(new { message = "User is not a member of this organization" });
            }

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
            _logger.LogError(ex, "Error during Entra ID authentication");
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

public class AwsLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public class EntraIdLoginRequest
{
    public string UserPrincipalName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}