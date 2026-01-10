using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IChatroomRoleService
    {
        Task<ChatroomRole?> GetRoleByIdAsync(int roleId);
        Task<ChatroomRole> CreateRoleAsync(int chatroomId, string userId, string roleName, bool canDeleteMessages, bool canBanUsers, bool canManageRoles);
        Task<bool> UpdateRoleAsync(int roleId, string userId, string? roleName, bool? canDeleteMessages, bool? canBanUsers, bool? canManageRoles);
        Task<bool> DeleteRoleAsync(int roleId, string userId);
        Task<IEnumerable<ChatroomRole>> GetChatroomRolesAsync(int chatroomId);
        Task<bool> AssignRoleToMemberAsync(int roleId, int chatroomId, string targetUserId, string assigningUserId);
        Task<bool> RemoveRoleFromMemberAsync(int roleId, int chatroomId, string targetUserId, string removingUserId);
    }
}
