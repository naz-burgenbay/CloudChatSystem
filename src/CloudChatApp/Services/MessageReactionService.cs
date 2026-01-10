using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class MessageReactionService : IMessageReactionService
    {
        private readonly ApplicationDbContext _context;

        public MessageReactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MessageReaction> AddReactionAsync(int messageId, string userId, string emoji)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be null or empty.", nameof(emoji));

            // Verify message exists and get chatroom
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
            if (message == null)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            // Verify user is a member of the chatroom
            var isMember = await _context.ChatroomMembers
                .AnyAsync(cm => cm.ChatroomId == message.ChatroomId && cm.UserId == userId);
            if (!isMember)
                throw new InvalidOperationException($"User is not a member of this chatroom.");

            // Check if user already reacted with this emoji
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId && mr.Emoji == emoji);

            if (existingReaction != null)
                throw new InvalidOperationException($"User already reacted with this emoji.");

            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                ReactedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            return reaction;
        }

        public async Task<bool> RemoveReactionAsync(int messageId, string userId, string emoji)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be null or empty.", nameof(emoji));

            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId && mr.Emoji == emoji);

            if (reaction == null)
                throw new InvalidOperationException($"Reaction not found.");

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<MessageReaction>> GetReactionsAsync(int messageId)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            // Verify message exists
            var messageExists = await _context.Messages.AnyAsync(m => m.MessageId == messageId);
            if (!messageExists)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            return await _context.MessageReactions
                .Include(mr => mr.User)
                .Where(mr => mr.MessageId == messageId)
                .OrderByDescending(mr => mr.ReactedAt)
                .ToListAsync();
        }

        public async Task<int> GetReactionCountAsync(int messageId, string emoji)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be null or empty.", nameof(emoji));

            return await _context.MessageReactions
                .CountAsync(mr => mr.MessageId == messageId && mr.Emoji == emoji);
        }

        public async Task<bool> HasUserReactedAsync(int messageId, string userId, string emoji)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(emoji))
                throw new ArgumentException("Emoji cannot be null or empty.", nameof(emoji));

            return await _context.MessageReactions
                .AnyAsync(mr => mr.MessageId == messageId && mr.UserId == userId && mr.Emoji == emoji);
        }

        public async Task<Dictionary<string, int>> GetReactionSummaryAsync(int messageId)
        {
            if (messageId <= 0)
                throw new ArgumentException("Message ID must be greater than 0.", nameof(messageId));

            // Verify message exists
            var messageExists = await _context.Messages.AnyAsync(m => m.MessageId == messageId);
            if (!messageExists)
                throw new InvalidOperationException($"Message with ID '{messageId}' not found.");

            var reactions = await _context.MessageReactions
                .Where(mr => mr.MessageId == messageId)
                .GroupBy(mr => mr.Emoji)
                .Select(g => new { Emoji = g.Key, Count = g.Count() })
                .ToListAsync();

            return reactions.ToDictionary(r => r.Emoji, r => r.Count);
        }
    }
}
