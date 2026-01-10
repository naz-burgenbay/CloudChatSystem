using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class ChatroomService : IChatroomService
    {
        private readonly ApplicationDbContext _context;

        public ChatroomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Chatroom?> GetChatroomByIdAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            return await _context.Chatrooms
                .Include(c => c.Members)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
        }

        public async Task<Chatroom> CreateChatroomAsync(string userId, string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Chatroom name cannot be null or empty.", nameof(name));

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            var chatroom = new Chatroom
            {
                Name = name,
                Description = description,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Chatrooms.Add(chatroom);
            await _context.SaveChangesAsync();

            // Add creator as member
            var member = new ChatroomMember
            {
                ChatroomId = chatroom.ChatroomId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            _context.ChatroomMembers.Add(member);
            await _context.SaveChangesAsync();

            return chatroom;
        }

        public async Task<bool> DeleteChatroomAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            _context.Chatrooms.Remove(chatroom);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateChatroomAsync(int chatroomId, string? name, string? description)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            if (!string.IsNullOrWhiteSpace(name))
                chatroom.Name = name;

            if (description != null)
                chatroom.Description = description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TransferOwnershipAsync(int chatroomId, string newOwnerId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(newOwnerId))
                throw new ArgumentException("New owner ID cannot be null or empty.", nameof(newOwnerId));

            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            // Verify new owner exists
            var newOwnerExists = await _context.Users.AnyAsync(u => u.Id == newOwnerId);
            if (!newOwnerExists)
                throw new InvalidOperationException($"User with ID '{newOwnerId}' not found.");

            // Verify new owner is a member
            var isMember = await _context.ChatroomMembers
                .AnyAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == newOwnerId);
            if (!isMember)
                throw new InvalidOperationException($"User must be a chatroom member to receive ownership.");

            chatroom.CreatedByUserId = newOwnerId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Chatroom>> GetUserChatroomsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.Chatrooms
                .Include(c => c.Members)
                .Where(c => c.Members.Any(m => m.UserId == userId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Chatroom>> GetOwnedChatroomsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.Chatrooms
                .Include(c => c.Members)
                .Where(c => c.CreatedByUserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Chatroom?> GetChatroomByIdAsync(string chatroomId)
        {
            if (string.IsNullOrWhiteSpace(chatroomId) || !int.TryParse(chatroomId, out int id))
                throw new ArgumentException("Chatroom ID must be a valid number.", nameof(chatroomId));

            return await GetChatroomByIdAsync(id);
        }

        public async Task<bool> DeleteChatroomAsync(string chatroomId)
        {
            if (string.IsNullOrWhiteSpace(chatroomId) || !int.TryParse(chatroomId, out int id))
                throw new ArgumentException("Chatroom ID must be a valid number.", nameof(chatroomId));

            return await DeleteChatroomAsync(id);
        }

        public async Task<bool> UpdateChatroomAsync(string chatroomId, string? name, string? description)
        {
            if (string.IsNullOrWhiteSpace(chatroomId) || !int.TryParse(chatroomId, out int id))
                throw new ArgumentException("Chatroom ID must be a valid number.", nameof(chatroomId));

            return await UpdateChatroomAsync(id, name, description);
        }

        public async Task<bool> TransferOwnershipAsync(string chatroomId, string newOwnerId)
        {
            if (string.IsNullOrWhiteSpace(chatroomId) || !int.TryParse(chatroomId, out int id))
                throw new ArgumentException("Chatroom ID must be a valid number.", nameof(chatroomId));

            return await TransferOwnershipAsync(id, newOwnerId);
        }
    }
}
