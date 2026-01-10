using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetMessageByIdAsync(int messageId)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Reactions)
                .Include(m => m.ReplyToMessage)
                .Include(m => m.Replies)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<Message> SendMessageAsync(int chatroomId, string userId, string content, int? replyToMessageId = null)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be null or empty.", nameof(content));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            // Verify user is a member of the chatroom
            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);
            if (member == null)
                throw new InvalidOperationException($"User is not a member of this chatroom.");

            // Check if user is muted
            if (member.IsMuted)
                throw new InvalidOperationException($"User is muted in this chatroom.");

            // If replying, validate reply target
            if (replyToMessageId.HasValue && replyToMessageId > 0)
            {
                var replyToMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == replyToMessageId);

                if (replyToMessage == null)
                    throw new InvalidOperationException($"Message to reply to with ID '{replyToMessageId}' not found.");

                if (replyToMessage.ChatroomId != chatroomId)
                    throw new InvalidOperationException($"Cannot reply to a message from a different chatroom.");

                // Prevent cycles: check if reply target is already a reply to this message
                if (await IsReplyChainAsync(replyToMessageId.Value, null))
                {
                    throw new InvalidOperationException($"Reply would create a cycle. Cannot reply to a message that is a reply to this chain.");
                }
            }

            var message = new Message
            {
                ChatroomId = chatroomId,
                UserId = userId,
                Content = content,
                ReplyToMessageId = replyToMessageId,
                SentAt = DateTime.UtcNow,
                EditedAt = null
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<bool> EditMessageAsync(int messageId, string userId, string newContent)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Message content cannot be null or empty.", nameof(newContent));

            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            if (message.UserId != userId)
                throw new InvalidOperationException($"Only the message author can edit this message.");

            message.Content = newContent;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var message = await _context.Messages
                .Include(m => m.Chatroom)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
            
            if (message == null)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            // Message author can always delete their own message
            if (message.UserId == userId)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                return true;
            }

            // Get chatroom owner and user's roles
            var chatroom = message.Chatroom;
            var isOwner = chatroom.CreatedByUserId == userId;

            if (isOwner)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                return true;
            }

            // Check if user has CanDeleteMessages permission through roles
            var hasDeletePermission = await _context.ChatroomMemberRoles
                .Include(cmr => cmr.ChatroomMember)
                .Include(cmr => cmr.ChatroomRole)
                .Where(cmr => cmr.ChatroomMember.UserId == userId && cmr.ChatroomMember.ChatroomId == message.ChatroomId)
                .AnyAsync(cmr => cmr.ChatroomRole.CanDeleteMessages);

            if (!hasDeletePermission)
                throw new InvalidOperationException($"You don't have permission to delete this message.");

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Message>> GetChatroomMessagesAsync(int chatroomId, int skip = 0, int take = 50)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (skip < 0)
                throw new ArgumentException("Skip value cannot be negative.", nameof(skip));

            if (take <= 0 || take > 100)
                throw new ArgumentException("Take value must be between 1 and 100.", nameof(take));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Reactions)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.ChatroomId == chatroomId)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessageRepliesAsync(int messageId)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            // Verify message exists
            var messageExists = await _context.Messages.AnyAsync(m => m.MessageId == messageId);
            if (!messageExists)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            return await _context.Messages
                .Include(m => m.User)
                .Where(m => m.ReplyToMessageId == messageId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        private async Task<bool> IsReplyChainAsync(int messageId, int? originalMessageId)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null || !message.ReplyToMessageId.HasValue)
                return false;

            if (message.ReplyToMessageId == originalMessageId)
                return true;

            return await IsReplyChainAsync(message.ReplyToMessageId.Value, originalMessageId ?? messageId);
        }
    }
}
