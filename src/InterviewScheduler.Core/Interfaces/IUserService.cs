using InterviewScheduler.Core.Entities;

namespace InterviewScheduler.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetCurrentUserAsync();
    Task<User> GetOrCreateUserAsync(string googleUserId, string email, string name);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByGoogleUserIdAsync(string googleUserId);
    Task UpdateLastLoginAsync(int userId);
}