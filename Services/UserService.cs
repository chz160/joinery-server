using JoineryServer.Data;
using JoineryServer.Models;
using Microsoft.EntityFrameworkCore;

namespace JoineryServer.Services;

public class UserService : IUserService
{
    private readonly JoineryDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(JoineryDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(string externalId, string username, string email, string authProvider, string? fullName = null)
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

    public async Task<User?> GetUserByExternalIdAsync(string externalId, string authProvider)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.AuthProvider == authProvider);
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}