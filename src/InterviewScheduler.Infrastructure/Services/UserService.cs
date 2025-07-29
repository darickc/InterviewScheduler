using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InterviewScheduler.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var googleUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(googleUserId))
        {
            return null;
        }

        return await GetUserByGoogleUserIdAsync(googleUserId);
    }

    public async Task<User> GetOrCreateUserAsync(string googleUserId, string email, string name)
    {
        var existingUser = await GetUserByGoogleUserIdAsync(googleUserId);
        if (existingUser != null)
        {
            // Update last login time
            await UpdateLastLoginAsync(existingUser.Id);
            return existingUser;
        }

        // Create new user
        var newUser = new User
        {
            GoogleUserId = googleUserId,
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return newUser;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByGoogleUserIdAsync(string googleUserId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleUserId == googleUserId);
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}