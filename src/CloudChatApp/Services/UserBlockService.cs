using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly ApplicationDbContext _context;

        public UserBlockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BlockUserAsync(string blockingUserId, string blockedUserId)
        {
            if (string.IsNullOrWhiteSpace(blockingUserId))
                throw new ArgumentException("Blocking user ID cannot be null or empty.", nameof(blockingUserId));

            if (string.IsNullOrWhiteSpace(blockedUserId))
                throw new ArgumentException("Blocked user ID cannot be null or empty.", nameof(blockedUserId));

            if (blockingUserId == blockedUserId)
                throw new InvalidOperationException("User cannot block themselves.");

            // Check if users exist
            var blockingUserExists = await _context.Users.AnyAsync(u => u.Id == blockingUserId);
            if (!blockingUserExists)
                throw new InvalidOperationException($"Blocking user with ID '{blockingUserId}' not found.");

            var blockedUserExists = await _context.Users.AnyAsync(u => u.Id == blockedUserId);
            if (!blockedUserExists)
                throw new InvalidOperationException($"User to block with ID '{blockedUserId}' not found.");

            // Check if block already exists
            var existingBlock = await _context.UserBlocks
                .FirstOrDefaultAsync(ub => ub.BlockingUserId == blockingUserId && ub.BlockedUserId == blockedUserId);

            if (existingBlock != null)
                throw new InvalidOperationException($"User '{blockedUserId}' is already blocked.");

            var block = new UserBlock
            {
                BlockingUserId = blockingUserId,
                BlockedUserId = blockedUserId,
                BlockedAt = DateTime.UtcNow
            };

            _context.UserBlocks.Add(block);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnblockUserAsync(string blockingUserId, string blockedUserId)
        {
            if (string.IsNullOrWhiteSpace(blockingUserId))
                throw new ArgumentException("Blocking user ID cannot be null or empty.", nameof(blockingUserId));

            if (string.IsNullOrWhiteSpace(blockedUserId))
                throw new ArgumentException("Blocked user ID cannot be null or empty.", nameof(blockedUserId));

            var block = await _context.UserBlocks
                .FirstOrDefaultAsync(ub => ub.BlockingUserId == blockingUserId && ub.BlockedUserId == blockedUserId);

            if (block == null)
                throw new InvalidOperationException($"No block relationship found between users '{blockingUserId}' and '{blockedUserId}'.");

            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserBlockedAsync(string blockingUserId, string blockedUserId)
        {
            if (string.IsNullOrWhiteSpace(blockingUserId))
                throw new ArgumentException("Blocking user ID cannot be null or empty.", nameof(blockingUserId));

            if (string.IsNullOrWhiteSpace(blockedUserId))
                throw new ArgumentException("Blocked user ID cannot be null or empty.", nameof(blockedUserId));

            return await _context.UserBlocks
                .AnyAsync(ub => ub.BlockingUserId == blockingUserId && ub.BlockedUserId == blockedUserId);
        }

        public async Task<IEnumerable<UserBlock>> GetBlockedUsersAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.UserBlocks
                .Include(ub => ub.BlockedUser)
                .Where(ub => ub.BlockingUserId == userId)
                .OrderByDescending(ub => ub.BlockedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserBlock>> GetUsersBlockingAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _context.UserBlocks
                .Include(ub => ub.BlockingUser)
                .Where(ub => ub.BlockedUserId == userId)
                .OrderByDescending(ub => ub.BlockedAt)
                .ToListAsync();
        }
    }
}
