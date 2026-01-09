using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            // Check if user is creator of any chatrooms that need ownership transfer
            var ownedChatrooms = await _context.Chatrooms
                .Where(c => c.CreatedByUserId == userId)
                .CountAsync();

            if (ownedChatrooms > 0)
            {
                throw new InvalidOperationException($"Cannot delete user. User owns {ownedChatrooms} chatroom(s). Transfer ownership before deletion.");
            }

            // Clean up user blocks where they are the blocker
            var blocksCreated = await _context.UserBlocks
                .Where(ub => ub.BlockingUserId == userId)
                .ToListAsync();
            _context.UserBlocks.RemoveRange(blocksCreated);

            // Clean up message reactions on other users' messages
            var reactions = await _context.MessageReactions
                .Where(mr => mr.UserId == userId)
                .ToListAsync();
            _context.MessageReactions.RemoveRange(reactions);

            await _context.SaveChangesAsync();

            // User deletion - cascades will handle removal of their messages (and reactions on those messages), their chatroom memberships, blocks against them
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, string? displayName, string? bio)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException($"User with ID '{userId}' not found.");

            user.DisplayName = displayName;
            user.Bio = bio;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user profile: {errors}");
            }

            return true;
        }
    }
}