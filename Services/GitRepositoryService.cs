using JoineryServer.Models;
using System.Text;
using System.Text.Json;

namespace JoineryServer.Services;

public interface IGitRepositoryService
{
    Task<List<GitQueryFile>> SyncRepositoryAsync(GitRepository repository);
    Task<GitQueryFile?> GetQueryFileAsync(GitRepository repository, string filePath);
    Task<List<string>> GetRepositoryFoldersAsync(GitRepository repository);
    Task<List<GitQueryFile>> GetQueryFilesInFolderAsync(GitRepository repository, string folderPath = "");
}

public class GitRepositoryService : IGitRepositoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitRepositoryService> _logger;

    public GitRepositoryService(HttpClient httpClient, ILogger<GitRepositoryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<GitQueryFile>> SyncRepositoryAsync(GitRepository repository)
    {
        _logger.LogInformation("Syncing repository {RepositoryUrl}", repository.RepositoryUrl);

        try
        {
            var queryFiles = new List<GitQueryFile>();

            // For now, we'll support GitHub repositories using the GitHub API
            if (repository.RepositoryUrl.Contains("github.com"))
            {
                queryFiles = await SyncGitHubRepositoryAsync(repository);
            }
            else
            {
                _logger.LogWarning("Repository type not supported: {RepositoryUrl}", repository.RepositoryUrl);
            }

            return queryFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing repository {RepositoryUrl}", repository.RepositoryUrl);
            return new List<GitQueryFile>();
        }
    }

    private async Task<List<GitQueryFile>> SyncGitHubRepositoryAsync(GitRepository repository)
    {
        var queryFiles = new List<GitQueryFile>();

        // Extract owner and repo name from GitHub URL
        var (owner, repoName) = ParseGitHubUrl(repository.RepositoryUrl);
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName))
        {
            _logger.LogError("Invalid GitHub URL: {RepositoryUrl}", repository.RepositoryUrl);
            return queryFiles;
        }

        // Configure HTTP client for GitHub API
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "JoineryServer/1.0");

        if (!string.IsNullOrEmpty(repository.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {repository.AccessToken}");
        }

        try
        {
            // Get repository contents recursively
            await GetRepositoryContentsRecursive(repository, owner, repoName, "", queryFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing GitHub repository {Owner}/{Repo}", owner, repoName);
        }

        return queryFiles;
    }

    private async Task GetRepositoryContentsRecursive(GitRepository repository, string owner, string repoName, string path, List<GitQueryFile> queryFiles)
    {
        var branch = repository.Branch ?? "main";
        var url = $"https://api.github.com/repos/{owner}/{repoName}/contents/{path}?ref={branch}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get contents for path {Path}: {StatusCode}", path, response.StatusCode);
            return;
        }

        var jsonContent = await response.Content.ReadAsStringAsync();
        var contents = JsonSerializer.Deserialize<GitHubContent[]>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (contents == null) return;

        foreach (var content in contents)
        {
            if (content.Type == "file" && IsQueryFile(content.Name))
            {
                var queryFile = await CreateQueryFileFromContent(repository, content, owner, repoName);
                if (queryFile != null)
                {
                    queryFiles.Add(queryFile);
                }
            }
            else if (content.Type == "dir")
            {
                await GetRepositoryContentsRecursive(repository, owner, repoName, content.Path, queryFiles);
            }
        }
    }

    private async Task<GitQueryFile?> CreateQueryFileFromContent(GitRepository repository, GitHubContent content, string owner, string repoName)
    {
        try
        {
            // Get file content
            var fileResponse = await _httpClient.GetAsync(content.DownloadUrl);
            if (!fileResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download file {Path}", content.Path);
                return null;
            }

            var sqlContent = await fileResponse.Content.ReadAsStringAsync();

            // Get commit information for the file
            var commitInfo = await GetLatestCommitForFile(owner, repoName, content.Path, repository.Branch ?? "main");

            return new GitQueryFile
            {
                GitRepositoryId = repository.Id,
                FilePath = content.Path,
                FileName = content.Name,
                SqlContent = sqlContent,
                DatabaseType = ExtractDatabaseTypeFromFileName(content.Name),
                Tags = ExtractTagsFromContent(sqlContent),
                LastCommitSha = content.Sha,
                LastCommitAuthor = commitInfo?.Author,
                LastCommitAt = commitInfo?.Date ?? DateTime.UtcNow,
                LastSyncAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating query file from content {Path}", content.Path);
            return null;
        }
    }

    private async Task<CommitInfo?> GetLatestCommitForFile(string owner, string repoName, string filePath, string branch)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repoName}/commits?path={filePath}&sha={branch}&per_page=1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var jsonContent = await response.Content.ReadAsStringAsync();
            var commits = JsonSerializer.Deserialize<GitHubCommit[]>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var latestCommit = commits?.FirstOrDefault();
            if (latestCommit?.Commit?.Author != null)
            {
                return new CommitInfo
                {
                    Author = latestCommit.Commit.Author.Name,
                    Date = latestCommit.Commit.Author.Date
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commit info for file {FilePath}", filePath);
        }

        return null;
    }

    public async Task<GitQueryFile?> GetQueryFileAsync(GitRepository repository, string filePath)
    {
        var queryFiles = await SyncRepositoryAsync(repository);
        return queryFiles.FirstOrDefault(qf => qf.FilePath == filePath);
    }

    public async Task<List<string>> GetRepositoryFoldersAsync(GitRepository repository)
    {
        var queryFiles = await SyncRepositoryAsync(repository);
        var folders = queryFiles
            .Select(qf => Path.GetDirectoryName(qf.FilePath)?.Replace("\\", "/") ?? "")
            .Where(folder => !string.IsNullOrEmpty(folder))
            .Distinct()
            .OrderBy(folder => folder)
            .ToList();

        return folders;
    }

    public async Task<List<GitQueryFile>> GetQueryFilesInFolderAsync(GitRepository repository, string folderPath = "")
    {
        var queryFiles = await SyncRepositoryAsync(repository);

        if (string.IsNullOrEmpty(folderPath))
        {
            return queryFiles.Where(qf => !qf.FilePath.Contains("/")).ToList();
        }

        var normalizedFolderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        return queryFiles
            .Where(qf =>
            {
                var fileDir = Path.GetDirectoryName(qf.FilePath)?.Replace("\\", "/") ?? "";
                return fileDir == normalizedFolderPath;
            })
            .ToList();
    }

    private static (string owner, string repoName) ParseGitHubUrl(string url)
    {
        try
        {
            // Support both https://github.com/owner/repo and git@github.com:owner/repo.git formats
            if (url.StartsWith("https://github.com/"))
            {
                var parts = url.Replace("https://github.com/", "").TrimEnd('/').Split('/');
                if (parts.Length >= 2)
                {
                    return (parts[0], parts[1].Replace(".git", ""));
                }
            }
            else if (url.StartsWith("git@github.com:"))
            {
                var repoPath = url.Replace("git@github.com:", "").Replace(".git", "");
                var parts = repoPath.Split('/');
                if (parts.Length >= 2)
                {
                    return (parts[0], parts[1]);
                }
            }
        }
        catch
        {
            // Fall through to return empty values
        }

        return ("", "");
    }

    private static bool IsQueryFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".sql" || extension == ".txt";
    }

    private static string? ExtractDatabaseTypeFromFileName(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();

        if (lowerName.Contains("postgres") || lowerName.Contains("pg")) return "PostgreSQL";
        if (lowerName.Contains("mysql")) return "MySQL";
        if (lowerName.Contains("sqlserver") || lowerName.Contains("mssql")) return "SQLServer";
        if (lowerName.Contains("sqlite")) return "SQLite";
        if (lowerName.Contains("oracle")) return "Oracle";

        return null;
    }

    private static List<string>? ExtractTagsFromContent(string content)
    {
        var tags = new List<string>();

        // Look for comment patterns that might contain tags
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Take(10)) // Only check first 10 lines for performance
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("--") && trimmedLine.Contains("tags:", StringComparison.OrdinalIgnoreCase))
            {
                var tagsPart = trimmedLine.Substring(trimmedLine.IndexOf("tags:", StringComparison.OrdinalIgnoreCase) + 5).Trim();
                var fileTags = tagsPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
                tags.AddRange(fileTags);
            }
        }

        return tags.Any() ? tags : null;
    }

    // Helper classes for JSON deserialization
    private class GitHubContent
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Sha { get; set; } = "";
        public string Type { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }

    private class GitHubCommit
    {
        public GitHubCommitDetails Commit { get; set; } = new();
    }

    private class GitHubCommitDetails
    {
        public GitHubAuthor Author { get; set; } = new();
    }

    private class GitHubAuthor
    {
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
    }

    private class CommitInfo
    {
        public string Author { get; set; } = "";
        public DateTime Date { get; set; }
    }
}