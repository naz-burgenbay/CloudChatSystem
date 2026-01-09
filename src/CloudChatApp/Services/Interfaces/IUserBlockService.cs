using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IUserBlockService
    {
        Task<bool> BlockUserAsync(string blockingUserId, string blockedUserId);
        Task<bool> UnblockUserAsync(string blockingUserId, string blockedUserId);
        Task<bool> IsUserBlockedAsync(string blockingUserId, string blockedUserId);
        Task<IEnumerable<UserBlock>> GetBlockedUsersAsync(string userId);
        Task<IEnumerable<UserBlock>> GetUsersBlockingAsync(string userId);
    }
}
