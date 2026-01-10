using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IChatroomService
    {
        Task<Chatroom?> GetChatroomByIdAsync(int chatroomId);
        Task<Chatroom> CreateChatroomAsync(string userId, string name, string? description);
        Task<bool> DeleteChatroomAsync(int chatroomId);
        Task<bool> UpdateChatroomAsync(int chatroomId, string? name, string? description);
        Task<bool> TransferOwnershipAsync(int chatroomId, string newOwnerId);
        Task<IEnumerable<Chatroom>> GetUserChatroomsAsync(string userId);
        Task<IEnumerable<Chatroom>> GetOwnedChatroomsAsync(string userId);
    }
}
