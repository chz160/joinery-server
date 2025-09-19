using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using JoineryServer.Models;

namespace JoineryServer.Services;

public interface IAwsIamService
{
    Task<bool> ValidateCredentialsAsync(string accessKeyId, string secretAccessKey, string region, string? roleArn = null, string? externalId = null);
    Task<List<AwsIamUser>> GetIamUsersAsync(OrganizationAwsIamConfig config);
    Task<AwsIamUser?> GetIamUserAsync(OrganizationAwsIamConfig config, string username);
}

public class AwsIamUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime? CreateDate { get; set; }
}

public class AwsIamService : IAwsIamService
{
    private readonly ILogger<AwsIamService> _logger;

    public AwsIamService(ILogger<AwsIamService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateCredentialsAsync(string accessKeyId, string secretAccessKey, string region, string? roleArn = null, string? externalId = null)
    {
        try
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            
            if (!string.IsNullOrEmpty(roleArn))
            {
                // Use STS to assume role
                var stsClient = new AmazonSecurityTokenServiceClient(accessKeyId, secretAccessKey, regionEndpoint);
                
                var assumeRoleRequest = new AssumeRoleRequest
                {
                    RoleArn = roleArn,
                    RoleSessionName = "JoineryServerValidation",
                    ExternalId = externalId
                };

                var assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest);
                
                // Use the temporary credentials to validate IAM access
                var tempCredentials = assumeRoleResponse.Credentials;
                var iamClient = new AmazonIdentityManagementServiceClient(
                    tempCredentials.AccessKeyId,
                    tempCredentials.SecretAccessKey,
                    tempCredentials.SessionToken,
                    regionEndpoint);

                await iamClient.GetAccountSummaryAsync();
            }
            else
            {
                // Direct IAM access
                var iamClient = new AmazonIdentityManagementServiceClient(accessKeyId, secretAccessKey, regionEndpoint);
                await iamClient.GetAccountSummaryAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate AWS credentials");
            return false;
        }
    }

    public async Task<List<AwsIamUser>> GetIamUsersAsync(OrganizationAwsIamConfig config)
    {
        var users = new List<AwsIamUser>();
        
        try
        {
            var iamClient = await CreateIamClientAsync(config);
            
            var request = new ListUsersRequest();
            ListUsersResponse? response = null;
            
            do
            {
                if (response != null)
                {
                    request.Marker = response.Marker;
                }
                
                response = await iamClient.ListUsersAsync(request);
                
                foreach (var user in response.Users)
                {
                    var awsUser = new AwsIamUser
                    {
                        Username = user.UserName,
                        UserId = user.UserId,
                        CreateDate = user.CreateDate,
                        Email = await GetUserEmailAsync(iamClient, user.UserName),
                        FullName = await GetUserFullNameAsync(iamClient, user.UserName)
                    };
                    
                    users.Add(awsUser);
                }
                
            } while (response.IsTruncated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get IAM users for organization {OrganizationId}", config.OrganizationId);
            throw;
        }
        
        return users;
    }

    public async Task<AwsIamUser?> GetIamUserAsync(OrganizationAwsIamConfig config, string username)
    {
        try
        {
            var iamClient = await CreateIamClientAsync(config);
            
            var response = await iamClient.GetUserAsync(new GetUserRequest { UserName = username });
            var user = response.User;
            
            return new AwsIamUser
            {
                Username = user.UserName,
                UserId = user.UserId,
                CreateDate = user.CreateDate,
                Email = await GetUserEmailAsync(iamClient, user.UserName),
                FullName = await GetUserFullNameAsync(iamClient, user.UserName)
            };
        }
        catch (NoSuchEntityException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get IAM user {Username} for organization {OrganizationId}", username, config.OrganizationId);
            throw;
        }
    }

    private async Task<AmazonIdentityManagementServiceClient> CreateIamClientAsync(OrganizationAwsIamConfig config)
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(config.AwsRegion);
        
        if (!string.IsNullOrEmpty(config.RoleArn))
        {
            // Use STS to assume role
            var stsClient = new AmazonSecurityTokenServiceClient(
                config.AccessKeyId, 
                config.SecretAccessKey, 
                regionEndpoint);
            
            var assumeRoleRequest = new AssumeRoleRequest
            {
                RoleArn = config.RoleArn,
                RoleSessionName = $"JoineryServer-{config.OrganizationId}",
                ExternalId = config.ExternalId
            };

            var assumeRoleResponse = await stsClient.AssumeRoleAsync(assumeRoleRequest);
            var tempCredentials = assumeRoleResponse.Credentials;
            
            return new AmazonIdentityManagementServiceClient(
                tempCredentials.AccessKeyId,
                tempCredentials.SecretAccessKey,
                tempCredentials.SessionToken,
                regionEndpoint);
        }
        else
        {
            // Direct IAM access
            return new AmazonIdentityManagementServiceClient(
                config.AccessKeyId, 
                config.SecretAccessKey, 
                regionEndpoint);
        }
    }

    private async Task<string> GetUserEmailAsync(AmazonIdentityManagementServiceClient iamClient, string username)
    {
        try
        {
            var response = await iamClient.ListUserTagsAsync(new ListUserTagsRequest { UserName = username });
            var emailTag = response.Tags.FirstOrDefault(t => t.Key.Equals("Email", StringComparison.OrdinalIgnoreCase));
            return emailTag?.Value ?? $"{username}@unknown.com";
        }
        catch
        {
            return $"{username}@unknown.com";
        }
    }

    private async Task<string?> GetUserFullNameAsync(AmazonIdentityManagementServiceClient iamClient, string username)
    {
        try
        {
            var response = await iamClient.ListUserTagsAsync(new ListUserTagsRequest { UserName = username });
            var nameTag = response.Tags.FirstOrDefault(t => t.Key.Equals("Name", StringComparison.OrdinalIgnoreCase)) ??
                         response.Tags.FirstOrDefault(t => t.Key.Equals("FullName", StringComparison.OrdinalIgnoreCase));
            return nameTag?.Value;
        }
        catch
        {
            return null;
        }
    }
}