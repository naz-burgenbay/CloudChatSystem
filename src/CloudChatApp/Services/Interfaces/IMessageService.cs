using CloudChatApp.Data.Entities;

namespace CloudChatApp.Services.Interfaces
{
    public interface IMessageService
    {
        Task<Message?> GetMessageByIdAsync(int messageId);
        Task<Message> SendMessageAsync(int chatroomId, string userId, string content, int? replyToMessageId = null);
        Task<bool> EditMessageAsync(int messageId, string userId, string newContent);
        Task<bool> DeleteMessageAsync(int messageId, string userId);
        Task<IEnumerable<Message>> GetChatroomMessagesAsync(int chatroomId, int skip = 0, int take = 50);
        Task<IEnumerable<Message>> GetMessageRepliesAsync(int messageId);
    }
}
