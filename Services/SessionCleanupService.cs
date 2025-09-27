using JoineryServer.Services;

namespace JoineryServer.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // Run every 30 minutes

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

                await CleanupExpiredSessionsAsync(sessionRepository);
                await CleanupRevokedSessionsAsync(sessionRepository);

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during session cleanup");
                // Wait a shorter interval before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(ISessionRepository sessionRepository)
    {
        var expiredSessions = await sessionRepository.GetExpiredSessionsAsync();

        if (expiredSessions.Any())
        {
            // Mark expired sessions as inactive
            foreach (var session in expiredSessions)
            {
                await sessionRepository.RevokeSessionAsync(session.SessionId, "Session expired - automatic cleanup");
            }

            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
    }

    private async Task CleanupRevokedSessionsAsync(ISessionRepository sessionRepository)
    {
        // Delete sessions that have been revoked for more than 30 days
        var oldRevokedSessions = await sessionRepository.GetExpiredSessionsAsync();
        var sessionsToDelete = oldRevokedSessions
            .Where(s => !s.IsActive && s.RevokedAt.HasValue && s.RevokedAt.Value < DateTime.UtcNow.AddDays(-30))
            .Select(s => s.SessionId)
            .ToList();

        if (sessionsToDelete.Any())
        {
            await sessionRepository.DeleteSessionsAsync(sessionsToDelete);
            _logger.LogInformation("Deleted {Count} old revoked sessions", sessionsToDelete.Count);
        }
    }
}