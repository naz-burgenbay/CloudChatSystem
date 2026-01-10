using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class ChatroomRoleService : IChatroomRoleService
    {
        private readonly ApplicationDbContext _context;

        public ChatroomRoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<bool> HasManageRolePermissionAsync(int chatroomId, string userId)
        {
            // Owner always has permission
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom?.CreatedByUserId == userId)
                return true;

            // Check if user has CanManageRoles permission through ChatroomMemberRole join table
            var hasPermission = await _context.ChatroomMemberRoles
                .Include(cmr => cmr.ChatroomMember)
                .Include(cmr => cmr.ChatroomRole)
                .Where(cmr => cmr.ChatroomMember.UserId == userId && cmr.ChatroomMember.ChatroomId == chatroomId)
                .AnyAsync(cmr => cmr.ChatroomRole.CanManageRoles);

            return hasPermission;
        }

        public async Task<ChatroomRole?> GetRoleByIdAsync(int roleId)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than 0.", nameof(roleId));

            return await _context.ChatroomRoles
                .Include(cr => cr.Chatroom)
                .FirstOrDefaultAsync(cr => cr.Id == roleId);
        }

        public async Task<ChatroomRole> CreateRoleAsync(int chatroomId, string userId, string roleName, bool canDeleteMessages, bool canBanUsers, bool canManageRoles)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));

            // Verify chatroom exists and user has permission to manage roles
            var chatroom = await _context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroomId);
            if (chatroom == null)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            var hasPermission = await HasManageRolePermissionAsync(chatroomId, userId);
            if (!hasPermission)
                throw new InvalidOperationException($"You don't have permission to create roles in this chatroom.");

            var role = new ChatroomRole
            {
                ChatroomId = chatroomId,
                Name = roleName,
                CanDeleteMessages = canDeleteMessages,
                CanBanUsers = canBanUsers,
                CanManageRoles = canManageRoles
            };

            _context.ChatroomRoles.Add(role);
            await _context.SaveChangesAsync();

            return role;
        }

        public async Task<bool> UpdateRoleAsync(int roleId, string userId, string? roleName, bool? canDeleteMessages, bool? canBanUsers, bool? canManageRoles)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than 0.", nameof(roleId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var role = await _context.ChatroomRoles
                .Include(cr => cr.Chatroom)
                .FirstOrDefaultAsync(cr => cr.Id == roleId);

            if (role == null)
                throw new InvalidOperationException($"Role with ID '{roleId}' not found.");

            var hasPermission = await HasManageRolePermissionAsync(role.ChatroomId, userId);
            if (!hasPermission)
                throw new InvalidOperationException($"You don't have permission to update roles in this chatroom.");

            if (!string.IsNullOrWhiteSpace(roleName))
                role.Name = roleName;

            if (canDeleteMessages.HasValue)
                role.CanDeleteMessages = canDeleteMessages.Value;

            if (canBanUsers.HasValue)
                role.CanBanUsers = canBanUsers.Value;

            if (canManageRoles.HasValue)
                role.CanManageRoles = canManageRoles.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoleAsync(int roleId, string userId)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than 0.", nameof(roleId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var role = await _context.ChatroomRoles
                .Include(cr => cr.Chatroom)
                .FirstOrDefaultAsync(cr => cr.Id == roleId);

            if (role == null)
                throw new InvalidOperationException($"Role with ID '{roleId}' not found.");

            var hasPermission = await HasManageRolePermissionAsync(role.ChatroomId, userId);
            if (!hasPermission)
                throw new InvalidOperationException($"You don't have permission to delete roles in this chatroom.");

            _context.ChatroomRoles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ChatroomRole>> GetChatroomRolesAsync(int chatroomId)
        {
            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            // Verify chatroom exists
            var chatroomExists = await _context.Chatrooms.AnyAsync(c => c.ChatroomId == chatroomId);
            if (!chatroomExists)
                throw new InvalidOperationException($"Chatroom with ID '{chatroomId}' not found.");

            return await _context.ChatroomRoles
                .Where(cr => cr.ChatroomId == chatroomId)
                .ToListAsync();
        }

        public async Task<bool> AssignRoleToMemberAsync(int roleId, int chatroomId, string targetUserId, string assigningUserId)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than 0.", nameof(roleId));

            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(assigningUserId))
                throw new ArgumentException("Assigning user ID cannot be null or empty.", nameof(assigningUserId));

            // Verify role exists and belongs to chatroom
            var role = await _context.ChatroomRoles.FirstOrDefaultAsync(cr => cr.Id == roleId && cr.ChatroomId == chatroomId);
            if (role == null)
                throw new InvalidOperationException($"Role with ID '{roleId}' not found in this chatroom.");

            // Verify assigning user has permission to manage roles
            var hasPermission = await HasManageRolePermissionAsync(chatroomId, assigningUserId);
            if (!hasPermission)
                throw new InvalidOperationException($"You don't have permission to assign roles in this chatroom.");

            // Verify target user is a member
            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == targetUserId);
            if (member == null)
                throw new InvalidOperationException($"Target user is not a member of this chatroom.");

            // Check if assignment already exists
            var existingAssignment = await _context.ChatroomMemberRoles
                .FirstOrDefaultAsync(cmr => cmr.ChatroomMemberId == member.ChatroomMemberId && cmr.ChatroomRoleId == roleId);

            if (existingAssignment != null)
                throw new InvalidOperationException($"User already has this role assigned.");

            var assignment = new ChatroomMemberRole
            {
                ChatroomMemberId = member.ChatroomMemberId,
                ChatroomRoleId = roleId,
                AssignedAt = DateTime.UtcNow
            };

            _context.ChatroomMemberRoles.Add(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromMemberAsync(int roleId, int chatroomId, string targetUserId, string removingUserId)
        {
            if (roleId <= 0)
                throw new ArgumentException("Role ID must be greater than 0.", nameof(roleId));

            if (chatroomId <= 0)
                throw new ArgumentException("Chatroom ID must be greater than 0.", nameof(chatroomId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (string.IsNullOrWhiteSpace(removingUserId))
                throw new ArgumentException("Removing user ID cannot be null or empty.", nameof(removingUserId));

            // Verify role exists and belongs to chatroom
            var role = await _context.ChatroomRoles.FirstOrDefaultAsync(cr => cr.Id == roleId && cr.ChatroomId == chatroomId);
            if (role == null)
                throw new InvalidOperationException($"Role with ID '{roleId}' not found in this chatroom.");

            // Verify removing user has permission to manage roles
            var hasPermission = await HasManageRolePermissionAsync(chatroomId, removingUserId);
            if (!hasPermission)
                throw new InvalidOperationException($"You don't have permission to remove roles in this chatroom.");

            // Verify target user is a member
            var member = await _context.ChatroomMembers
                .FirstOrDefaultAsync(cm => cm.ChatroomId == chatroomId && cm.UserId == targetUserId);
            if (member == null)
                throw new InvalidOperationException($"Target user is not a member of this chatroom.");

            var assignment = await _context.ChatroomMemberRoles
                .FirstOrDefaultAsync(cmr => cmr.ChatroomMemberId == member.ChatroomMemberId && cmr.ChatroomRoleId == roleId);

            if (assignment == null)
                throw new InvalidOperationException($"User does not have this role assigned.");

            _context.ChatroomMemberRoles.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
