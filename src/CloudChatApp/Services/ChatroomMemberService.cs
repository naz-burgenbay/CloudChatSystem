using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class ChatroomMemberService : IChatroomMemberService
    {
        private readonly ApplicationDbContext _context;

        public ChatroomMemberService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatroomMember?> GetMemberAsync(int chatroomId, string userId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.ChatroomMembers
                .Include(cm => cm.User)
                .Include(cm => cm.Chatroom)
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);
        }

        public async Task<bool> AddMemberAsync(int chatroomId, string userId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            // Check if user is banned from this chatroom
            var isBanned = await _context.UserBans
                .AnyAsync(ub => ub.ChatroomId == chatroomId && ub.BannedUserId == userId);
            if (isBanned)
                throw new InvalidOperationException($"User is banned from this chatroom.");

            // Check if already a member
            var existingMember = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);

            if (existingMember != null)
                throw new InvalidOperationException($"User is already a member of this chatroom.");

            var member = new ChatroomMember
            {
                ChatroomId = chatroomId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            _context.ChatroomMembers.Add(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int chatroomId, string userId, string? removingUserId = null)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            // If removingUserId not provided, user is removing themselves
            var isRemovingSelf = removingUserId == null || removingUserId == userId;
            if (!isRemovingSelf && string.IsNullOrWhiteSpace(removingUserId))
                throw new ArgumentException("Removing user ID cannot be null or empty.", nameof(removingUserId));

            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);

            if (member == null)
                throw new InvalidOperationException($"User is not a member of this chatroom.");

            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            // Owner cannot leave
            if (isRemovingSelf && chatroom.CreatedByUserId == userId)
                throw new InvalidOperationException($"Chatroom owner cannot leave. Transfer ownership first.");

            // If someone else is removing this user, check permissions
            if (!isRemovingSelf)
            {
                // Only owner or users with CanManageRoles can remove other members
                var isOwner = chatroom.CreatedByUserId == removingUserId;
                
                if (!isOwner)
                {
                    var hasManagePermission = await _context.ChatroomMemberRoles
                        .Include(cmr => cmr.ChatroomMember)
                        .Include(cmr => cmr.ChatroomRole)
                        .Where(cmr => cmr.ChatroomMember.UserId == removingUserId && cmr.ChatroomMember.ChatroomId == chatroomId)
                        .AnyAsync(cmr => cmr.ChatroomRole.CanManageRoles);

                    if (!hasManagePermission)
                        throw new InvalidOperationException($"You don't have permission to remove members from this chatroom.");
                }

                // Cannot remove the owner
                if (chatroom.CreatedByUserId == userId)
                    throw new InvalidOperationException($"Cannot remove the chatroom owner.");
            }

            _context.ChatroomMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ChatroomMember>> GetChatroomMembersAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            return await _context.ChatroomMembers
                .Include(cm => cm.User)
                .Where(cm => cm.ChatroomId == chatroomId)
                .OrderByDescending(cm => cm.JoinedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatroomMember>> GetUserMembershipsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            return await _context.ChatroomMembers
                .Include(cm => cm.Chatroom)
                .Where(cm => cm.UserId == userId)
                .OrderByDescending(cm => cm.JoinedAt)
                .ToListAsync();
        }

        public async Task<bool> IsMemberAsync(int chatroomId, string userId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.ChatroomMembers
                .AnyAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);
        }

        public async Task<int> GetMemberCountAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            return await _context.ChatroomMembers
                .CountAsync(cm => cm.ChatroomId == chatroomId);
        }
    }
}
