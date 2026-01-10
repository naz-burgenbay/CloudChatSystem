using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class ModerationService : IModerationService
    {
        private readonly ApplicationDbContext _context;

        public ModerationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> MuteMemberAsync(int chatroomId, string targetUserId, string actionUserId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(actionUserId))
                throw new ArgumentException("Action user ID cannot be null or empty.", nameof(actionUserId));

            // Verify user has moderation permission (owner or CanManageRoles)
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            var isOwner = chatroom.CreatedByUserId == actionUserId;
            if (!isOwner)
            {
                var hasManagePermission = await _context.ChatroomMemberRoles
                    .Include(cmr => cmr.ChatroomMember)
                    .Include(cmr => cmr.ChatroomRole)
                    .Where(cmr => cmr.ChatroomMember.UserId == actionUserId && cmr.ChatroomMember.ChatroomId == chatroomId)
                    .AnyAsync(cmr => cmr.ChatroomRole.CanManageRoles);

                if (!hasManagePermission)
                    throw new InvalidOperationException($"You don't have permission to mute members in this chatroom.");
            }

            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == targetUserId);

            if (member == null)
                throw new InvalidOperationException($"User is not a member of this chatroom.");

            if (member.IsMuted)
                throw new InvalidOperationException($"User is already muted.");

            member.IsMuted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnmuteMemberAsync(int chatroomId, string targetUserId, string actionUserId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(actionUserId))
                throw new ArgumentException("Action user ID cannot be null or empty.", nameof(actionUserId));

            // Verify user has moderation permission
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            var isOwner = chatroom.CreatedByUserId == actionUserId;
            if (!isOwner)
            {
                var hasManagePermission = await _context.ChatroomMemberRoles
                    .Include(cmr => cmr.ChatroomMember)
                    .Include(cmr => cmr.ChatroomRole)
                    .Where(cmr => cmr.ChatroomMember.UserId == actionUserId && cmr.ChatroomMember.ChatroomId == chatroomId)
                    .AnyAsync(cmr => cmr.ChatroomRole.CanManageRoles);

                if (!hasManagePermission)
                    throw new InvalidOperationException($"You don't have permission to unmute members in this chatroom.");
            }

            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == targetUserId);

            if (member == null)
                throw new InvalidOperationException($"User is not a member of this chatroom.");

            if (!member.IsMuted)
                throw new InvalidOperationException($"User is not muted.");

            member.IsMuted = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsMutedAsync(int chatroomId, string userId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == userId);

            return member?.IsMuted ?? false;
        }

        public async Task<bool> BanUserAsync(int chatroomId, string targetUserId, string actionUserId, string? reason = null)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(actionUserId))
                throw new ArgumentException("Action user ID cannot be null or empty.", nameof(actionUserId));

            // Verify user has ban permission (owner or CanBanUsers)
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            var isOwner = chatroom.CreatedByUserId == actionUserId;
            if (!isOwner)
            {
                var hasBanPermission = await _context.ChatroomMemberRoles
                    .Include(cmr => cmr.ChatroomMember)
                    .Include(cmr => cmr.ChatroomRole)
                    .Where(cmr => cmr.ChatroomMember.UserId == actionUserId && cmr.ChatroomMember.ChatroomId == chatroomId)
                    .AnyAsync(cmr => cmr.ChatroomRole.CanBanUsers);

                if (!hasBanPermission)
                    throw new InvalidOperationException($"You don't have permission to ban users in this chatroom.");
            }

            // Cannot ban the owner
            if (chatroom.CreatedByUserId == targetUserId)
                throw new InvalidOperationException($"Cannot ban the chatroom owner.");

            // Check if already banned
            var existingBan = await _context.UserBans
                .FirstOrDefaultAsync(ub => ub.ChatroomId == chatroomId && ub.BannedUserId == targetUserId);

            if (existingBan != null)
                throw new InvalidOperationException($"User is already banned from this chatroom.");

            var ban = new UserBan
            {
                ChatroomId = chatroomId,
                BannedUserId = targetUserId,
                BannedByUserId = actionUserId,
                Reason = reason,
                BannedAt = DateTime.UtcNow
            };

            _context.UserBans.Add(ban);

            // Remove user from chatroom if they're a member
            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == targetUserId);

            if (member != null)
                _context.ChatroomMembers.Remove(member);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnbanUserAsync(int chatroomId, string targetUserId, string actionUserId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(actionUserId))
                throw new ArgumentException("Action user ID cannot be null or empty.", nameof(actionUserId));

            // Verify user has ban permission
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            var isOwner = chatroom.CreatedByUserId == actionUserId;
            if (!isOwner)
            {
                var hasBanPermission = await _context.ChatroomMemberRoles
                    .Include(cmr => cmr.ChatroomMember)
                    .Include(cmr => cmr.ChatroomRole)
                    .Where(cmr => cmr.ChatroomMember.UserId == actionUserId && cmr.ChatroomMember.ChatroomId == chatroomId)
                    .AnyAsync(cmr => cmr.ChatroomRole.CanBanUsers);

                if (!hasBanPermission)
                    throw new InvalidOperationException($"You don't have permission to unban users in this chatroom.");
            }

            var ban = await _context.UserBans
                .FirstOrDefaultAsync(ub => ub.ChatroomId == chatroomId && ub.BannedUserId == targetUserId);

            if (ban == null)
                throw new InvalidOperationException($"User is not banned from this chatroom.");

            _context.UserBans.Remove(ban);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserBannedAsync(int chatroomId, string userId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.UserBans
                .AnyAsync(ub => ub.ChatroomId == chatroomId && ub.BannedUserId == userId);
        }

        public async Task<IEnumerable<UserBan>> GetChatroomBansAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            return await _context.UserBans
                .Include(ub => ub.BannedUser)
                .Include(ub => ub.BannedByUser)
                .Where(ub => ub.ChatroomId == chatroomId)
                .OrderByDescending(ub => ub.BannedAt)
                .ToListAsync();
        }
    }
}
