using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using JoineryServer.Models;

namespace JoineryServer.Services;

public interface IEntraIdService
{
    Task<bool> ValidateCredentialsAsync(string tenantId, string clientId, string clientSecret, string? domain = null);
    Task<List<EntraIdUser>> GetEntraIdUsersAsync(OrganizationEntraIdConfig config);
    Task<EntraIdUser?> GetEntraIdUserAsync(OrganizationEntraIdConfig config, string userPrincipalName);
}

public class EntraIdUser
{
    public string UserPrincipalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
}

public class EntraIdService : IEntraIdService
{
    private readonly ILogger<EntraIdService> _logger;

    public EntraIdService(ILogger<EntraIdService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateCredentialsAsync(string tenantId, string clientId, string clientSecret, string? domain = null)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(tenantId, clientId, clientSecret);

            // Test the connection by trying to get the organization info
            var organization = await graphClient.Organization.GetAsync();

            if (organization?.Value?.Count == 0)
            {
                return false;
            }

            // If domain is specified, validate it exists in the tenant
            if (!string.IsNullOrEmpty(domain))
            {
                var domains = await graphClient.Domains.GetAsync();
                var domainExists = domains?.Value?.Any(d => d.Id?.Equals(domain, StringComparison.OrdinalIgnoreCase) == true) == true;

                if (!domainExists)
                {
                    _logger.LogWarning("Domain {Domain} not found in tenant {TenantId}", domain, tenantId);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Entra ID credentials for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<List<EntraIdUser>> GetEntraIdUsersAsync(OrganizationEntraIdConfig config)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(config.TenantId, config.ClientId, config.ClientSecret);
            var users = await graphClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] { "id", "userPrincipalName", "displayName", "mail", "givenName", "surname", "createdDateTime" };
                requestConfiguration.QueryParameters.Top = 999;
            });

            var result = new List<EntraIdUser>();

            if (users?.Value != null)
            {
                foreach (var user in users.Value)
                {
                    // Filter by domain if specified
                    if (!string.IsNullOrEmpty(config.Domain) &&
                        !user.UserPrincipalName?.EndsWith($"@{config.Domain}", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        continue;
                    }

                    result.Add(new EntraIdUser
                    {
                        UserId = user.Id ?? string.Empty,
                        UserPrincipalName = user.UserPrincipalName ?? string.Empty,
                        DisplayName = user.DisplayName ?? string.Empty,
                        Email = user.Mail ?? user.UserPrincipalName ?? string.Empty,
                        GivenName = user.GivenName,
                        Surname = user.Surname,
                        CreatedDateTime = user.CreatedDateTime?.DateTime
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users from Entra ID for tenant {TenantId}", config.TenantId);
            throw;
        }
    }

    public async Task<EntraIdUser?> GetEntraIdUserAsync(OrganizationEntraIdConfig config, string userPrincipalName)
    {
        try
        {
            var graphClient = await CreateGraphClientAsync(config.TenantId, config.ClientId, config.ClientSecret);

            var user = await graphClient.Users[userPrincipalName].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] { "id", "userPrincipalName", "displayName", "mail", "givenName", "surname", "createdDateTime" };
            });

            if (user == null)
            {
                return null;
            }

            // Filter by domain if specified
            if (!string.IsNullOrEmpty(config.Domain) &&
                !user.UserPrincipalName?.EndsWith($"@{config.Domain}", StringComparison.OrdinalIgnoreCase) == true)
            {
                return null;
            }

            return new EntraIdUser
            {
                UserId = user.Id ?? string.Empty,
                UserPrincipalName = user.UserPrincipalName ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                Email = user.Mail ?? user.UserPrincipalName ?? string.Empty,
                GivenName = user.GivenName,
                Surname = user.Surname,
                CreatedDateTime = user.CreatedDateTime?.DateTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserPrincipalName} from Entra ID for tenant {TenantId}",
                userPrincipalName, config.TenantId);
            return null;
        }
    }

    private Task<GraphServiceClient> CreateGraphClientAsync(string tenantId, string clientId, string clientSecret)
    {
        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

        var graphClient = new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });

        return Task.FromResult(graphClient);
    }
}