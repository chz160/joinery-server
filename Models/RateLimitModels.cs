namespace JoineryServer.Models;

public class RateLimitConfig
{
    public bool EnableDistributedCache { get; set; } = false;
    public RedisConfig Redis { get; set; } = new();
    public RateLimitTiers Global { get; set; } = new();
    public Dictionary<string, RateLimitTiers> Endpoints { get; set; } = new();
}

public class RedisConfig
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public class RateLimitTiers
{
    public RateLimitSettings Anonymous { get; set; } = new();
    public RateLimitSettings Authenticated { get; set; } = new();
    public RateLimitSettings Admin { get; set; } = new();
}

public class RateLimitSettings
{
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
}

public enum UserAuthLevel
{
    Anonymous,
    Authenticated,
    Admin
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetTime { get; set; }
    public int Limit { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public UserAuthLevel AuthLevel { get; set; }
    public TimeSpan RetryAfter { get; set; }
}