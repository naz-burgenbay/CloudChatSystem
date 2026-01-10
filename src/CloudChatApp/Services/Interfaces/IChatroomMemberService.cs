using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IChatroomMemberService
    {
        Task<ChatroomMember?> GetMemberAsync(int chatroomId, string userId);
        Task<bool> AddMemberAsync(int chatroomId, string userId);
        Task<bool> RemoveMemberAsync(int chatroomId, string userId, string? removingUserId = null);
        Task<IEnumerable<ChatroomMember>> GetChatroomMembersAsync(int chatroomId);
        Task<IEnumerable<ChatroomMember>> GetUserMembershipsAsync(string userId);
        Task<bool> IsMemberAsync(int chatroomId, string userId);
        Task<int> GetMemberCountAsync(int chatroomId);
    }
}
