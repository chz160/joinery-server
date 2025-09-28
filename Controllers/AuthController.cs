using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IGitHubAuthService _gitHubAuthService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ISessionService _sessionService;

    public AuthController(IConfiguration configuration, JoineryDbContext context, ILogger<AuthController> logger, IAwsIamService awsIamService, IEntraIdService entraIdService, IGitHubAuthService gitHubAuthService, IUserService userService, ITokenService tokenService, IRateLimitingService rateLimitingService, ISessionService sessionService)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _awsIamService = awsIamService;
        _entraIdService = entraIdService;
        _gitHubAuthService = gitHubAuthService;
        _userService = userService;
        _tokenService = tokenService;
        _rateLimitingService = rateLimitingService;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Initiate GitHub OAuth authentication
    /// </summary>
    [HttpGet("login/github")]
    public IActionResult LoginGitHub()
    {
        // Rate limiting check
        var clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!_rateLimitingService.IsAllowed(clientId, "auth/login/github"))
        {
            return StatusCode(429, new { message = "Rate limit exceeded" });
        }

        // Generate state parameter for CSRF protection
        var state = _gitHubAuthService.GenerateState();

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/callback/github"
        };

        // Store state in authentication properties for validation
        properties.Items["state"] = state;

        return Challenge(properties, "GitHub");
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

            // Validate state parameter for CSRF protection
            var storedState = authenticateResult.Properties?.Items.ContainsKey("state") == true
                ? authenticateResult.Properties.Items["state"]
                : null;
            var receivedState = HttpContext.Request.Query["state"].ToString();

            if (!_gitHubAuthService.ValidateState(storedState ?? "", receivedState))
            {
                _logger.LogWarning("Invalid state parameter received in GitHub callback");
                return BadRequest(new { message = "Invalid authentication state" });
            }

            var claims = authenticateResult.Principal?.Claims.ToList() ?? new List<Claim>();
            var githubId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(githubId))
            {
                return BadRequest(new { message = "Unable to get GitHub user information" });
            }

            var user = await _userService.GetOrCreateUserAsync(githubId, username ?? "unknown", email ?? "unknown@github.com", "GitHub");

            // Create session
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var session = await _sessionService.CreateSessionAsync(user.Id, ipAddress, userAgent, "GitHub");

            var token = _tokenService.GenerateAccessToken(user, session.SessionId);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            return Ok(new
            {
                access_token = token,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = _configuration.GetSection("JWT")["AccessTokenExpirationMinutes"] != null ? int.Parse(_configuration.GetSection("JWT")["AccessTokenExpirationMinutes"]!) * 60 : 15 * 60,
                session_id = session.SessionId,
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
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var (newAccessToken, userId) = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);
            if (newAccessToken == null || userId == null)
            {
                return BadRequest(new { message = "Invalid or expired refresh token" });
            }

            // Generate new refresh token (token rotation)
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(userId.Value);

            return Ok(new
            {
                access_token = newAccessToken,
                refresh_token = newRefreshToken.Token,
                token_type = "Bearer",
                expires_in = _configuration.GetSection("JWT")["AccessTokenExpirationMinutes"] != null ? int.Parse(_configuration.GetSection("JWT")["AccessTokenExpirationMinutes"]!) * 60 : 15 * 60
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "Internal server error during token refresh" });
        }
    }

    /// <summary>
    /// Revoke all refresh tokens for the current user
    /// </summary>
    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllTokens()
    {
        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _tokenService.RevokeAllUserTokensAsync(userId, "User requested revocation", clientIp);

            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
            return Ok(new { message = "All tokens have been revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, new { message = "Internal server error during token revocation" });
        }
    }

    /// <summary>
    /// Revoke a specific refresh token
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, "User requested revocation", clientIp);

            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, new { message = "Internal server error during token revocation" });
        }
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

            var user = await _userService.GetOrCreateUserAsync(microsoftId, username ?? "unknown", email ?? "unknown@microsoft.com", "Microsoft", fullName?.Trim());

            // Create session
            var ipAddress = GetClientIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var session = await _sessionService.CreateSessionAsync(user.Id, ipAddress, userAgent, "Microsoft");

            var token = _tokenService.GenerateAccessToken(user, session.SessionId);

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                session_id = session.SessionId,
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
            var user = await _userService.GetOrCreateUserAsync(awsUser.UserId, awsUser.Username, awsUser.Email, "AWS", awsUser.FullName);

            // Verify the user is a member of this organization
            var orgMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == organization.Id && m.UserId == user.Id);

            if (orgMember == null)
            {
                return BadRequest(new { message = "User is not a member of this organization" });
            }

            var token = _tokenService.GenerateAccessToken(user);

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

            var user = await _userService.GetOrCreateUserAsync(entraIdUser.UserId, entraIdUser.UserPrincipalName, entraIdUser.Email, "Microsoft", fullName);

            // Verify the user is a member of this organization
            var orgMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == organization.Id && m.UserId == user.Id);

            if (orgMember == null)
            {
                return BadRequest(new { message = "User is not a member of this organization" });
            }

            var token = _tokenService.GenerateAccessToken(user);

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

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
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