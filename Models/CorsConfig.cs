namespace JoineryServer.Models;

public class CorsConfig
{
    public bool EnableCors { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public List<string> AllowedHeaders { get; set; } = new() { "Content-Type", "Authorization", "X-API-Key" };
    public List<string> ExposedHeaders { get; set; } = new() { "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "X-RateLimit-Policy" };
    public bool AllowCredentials { get; set; } = true;
    public int PreflightMaxAge { get; set; } = 86400; // 24 hours in seconds
    public bool AllowAnyOriginInDevelopment { get; set; } = true;
}