using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class UserBlockServiceTests
    {
        [Fact]
        public async Task BlockUserAsync_NullBlockingUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.BlockUserAsync(null!, "user2"));
        }

        [Fact]
        public async Task BlockUserAsync_EmptyBlockingUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.BlockUserAsync("", "user2"));
        }

        [Fact]
        public async Task BlockUserAsync_NullBlockedUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.BlockUserAsync("user1", null!));
        }

        [Fact]
        public async Task BlockUserAsync_SameUserId_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BlockUserAsync("user1", "user1"));
        }

        [Fact]
        public async Task BlockUserAsync_BlockingUserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BlockUserAsync("nonexistent", "user2"));
        }

        [Fact]
        public async Task BlockUserAsync_BlockedUserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BlockUserAsync("user1", "nonexistent"));
        }

        [Fact]
        public async Task BlockUserAsync_AlreadyBlocked_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);

            var block = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user2", BlockedAt = DateTime.UtcNow };
            context.UserBlocks.Add(block);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BlockUserAsync("user1", "user2"));
        }

        [Fact]
        public async Task BlockUserAsync_ValidBlock_ReturnsTrue()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            var result = await service.BlockUserAsync("user1", "user2");

            Assert.True(result);
            var block = await context.UserBlocks.FirstOrDefaultAsync(ub => ub.BlockingUserId == "user1" && ub.BlockedUserId == "user2");
            Assert.NotNull(block);
        }

        [Fact]
        public async Task UnblockUserAsync_NullBlockingUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UnblockUserAsync(null!, "user2"));
        }

        [Fact]
        public async Task UnblockUserAsync_BlockNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UnblockUserAsync("user1", "user2"));
        }

        [Fact]
        public async Task UnblockUserAsync_ValidUnblock_ReturnsTrue()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);

            var block = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user2", BlockedAt = DateTime.UtcNow };
            context.UserBlocks.Add(block);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            var result = await service.UnblockUserAsync("user1", "user2");

            Assert.True(result);
            var removedBlock = await context.UserBlocks.FirstOrDefaultAsync(ub => ub.BlockingUserId == "user1" && ub.BlockedUserId == "user2");
            Assert.Null(removedBlock);
        }

        [Fact]
        public async Task IsUserBlockedAsync_BlockExists_ReturnsTrue()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);

            var block = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user2", BlockedAt = DateTime.UtcNow };
            context.UserBlocks.Add(block);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            var result = await service.IsUserBlockedAsync("user1", "user2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsUserBlockedAsync_BlockDoesNotExist_ReturnsFalse()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new UserBlockService(context);

            var result = await service.IsUserBlockedAsync("user1", "user2");

            Assert.False(result);
        }

        [Fact]
        public async Task GetBlockedUsersAsync_ReturnsAllBlockedUsers()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3", Email = "user3@test.com" };
            context.Users.AddRange(user1, user2, user3);

            var block1 = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user2", BlockedAt = DateTime.UtcNow };
            var block2 = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user3", BlockedAt = DateTime.UtcNow.AddMinutes(1) };
            context.UserBlocks.AddRange(block1, block2);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            var result = await service.GetBlockedUsersAsync("user1");

            Assert.Equal(2, result.Count());
            Assert.Contains(result, b => b.BlockedUserId == "user2");
            Assert.Contains(result, b => b.BlockedUserId == "user3");
        }

        [Fact]
        public async Task GetUsersBlockingAsync_ReturnsAllUsersBlockingTarget()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3", Email = "user3@test.com" };
            context.Users.AddRange(user1, user2, user3);

            var block1 = new UserBlock { BlockingUserId = "user1", BlockedUserId = "user3", BlockedAt = DateTime.UtcNow };
            var block2 = new UserBlock { BlockingUserId = "user2", BlockedUserId = "user3", BlockedAt = DateTime.UtcNow.AddMinutes(1) };
            context.UserBlocks.AddRange(block1, block2);
            await context.SaveChangesAsync();

            var service = new UserBlockService(context);

            var result = await service.GetUsersBlockingAsync("user3");

            Assert.Equal(2, result.Count());
            Assert.Contains(result, b => b.BlockingUserId == "user1");
            Assert.Contains(result, b => b.BlockingUserId == "user2");
        }
    }
}
