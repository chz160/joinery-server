using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JoineryServer.Authorization;

public class RequireScopeAttribute : Attribute, IAuthorizationRequirement
{
    public string RequiredScope { get; }

    public RequireScopeAttribute(string scope)
    {
        RequiredScope = scope;
    }
}

public class ScopeAuthorizationHandler : AuthorizationHandler<RequireScopeAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireScopeAttribute requirement)
    {
        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        var authType = context.User.Claims
            .FirstOrDefault(c => c.Type == "auth_type")?.Value;

        // For JWT tokens, assume full access (existing behavior)
        if (authType != "api_key")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // For API keys, check scopes
        var scopes = context.User.Claims
            .Where(c => c.Type == "scope")
            .Select(c => c.Value)
            .ToList();

        // Check for required scope or "admin" scope which grants all access
        if (scopes.Contains(requirement.RequiredScope) || scopes.Contains("admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Convenience attributes for common scopes
public class RequireReadScopeAttribute : RequireScopeAttribute
{
    public RequireReadScopeAttribute() : base("read") { }
}

public class RequireWriteScopeAttribute : RequireScopeAttribute
{
    public RequireWriteScopeAttribute() : base("write") { }
}

public class RequireAdminScopeAttribute : RequireScopeAttribute
{
    public RequireAdminScopeAttribute() : base("admin") { }
}