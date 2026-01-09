using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateUserProfileAsync(string userId, string? displayName, string? bio);
    }
}
