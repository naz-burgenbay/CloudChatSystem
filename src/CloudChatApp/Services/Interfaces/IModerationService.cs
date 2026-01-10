using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IModerationService
    {
        Task<bool> MuteMemberAsync(int chatroomId, string targetUserId, string actionUserId);
        Task<bool> UnmuteMemberAsync(int chatroomId, string targetUserId, string actionUserId);
        Task<bool> IsMutedAsync(int chatroomId, string userId);
        Task<bool> BanUserAsync(int chatroomId, string targetUserId, string actionUserId, string? reason = null);
        Task<bool> UnbanUserAsync(int chatroomId, string targetUserId, string actionUserId);
        Task<bool> IsUserBannedAsync(int chatroomId, string userId);
        Task<IEnumerable<UserBan>> GetChatroomBansAsync(int chatroomId);
    }
}
