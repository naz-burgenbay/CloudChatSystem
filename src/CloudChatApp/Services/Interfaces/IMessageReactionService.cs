using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IMessageReactionService
    {
        Task<MessageReaction> AddReactionAsync(int messageId, string userId, string emoji);
        Task<bool> RemoveReactionAsync(int messageId, string userId, string emoji);
        Task<IEnumerable<MessageReaction>> GetReactionsAsync(int messageId);
        Task<int> GetReactionCountAsync(int messageId, string emoji);
        Task<bool> HasUserReactedAsync(int messageId, string userId, string emoji);
        Task<Dictionary<string, int>> GetReactionSummaryAsync(int messageId);
    }
}
