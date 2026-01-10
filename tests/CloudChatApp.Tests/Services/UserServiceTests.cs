using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CloudChatApp.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public UserServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var service = new UserService(context, _userManagerMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteUserAsync(null!));
        }

        [Fact]
        public async Task DeleteUserAsync_WithEmptyUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var service = new UserService(context, _userManagerMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteUserAsync(""));
        }

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var service = new UserService(context, _userManagerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DeleteUserAsync("user123"));
            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_UserOwnsChatrooms_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser { Id = "user123", UserName = "testuser" };

            // Add a chatroom owned by user
            var chatroom = new Chatroom
            {
                ChatroomId = 1,
                Name = "Test Chat",
                CreatedByUserId = "user123"
            };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByIdAsync("user123"))
                .ReturnsAsync(user);

            var service = new UserService(context, _userManagerMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DeleteUserAsync("user123"));
            Assert.Contains("owns", exception.Message);
            Assert.Contains("chatroom", exception.Message.ToLower());
        }

        [Fact]
        public async Task DeleteUserAsync_SuccessfullyDeletesUserAndCleansUp()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser { Id = "user123", UserName = "testuser" };
            var otherUser = new ApplicationUser { Id = "user456", UserName = "otheruser" };

            // Add user blocks created by user
            var userBlock = new UserBlock
            {
                BlockingUserId = "user123",
                BlockedUserId = "user456"
            };
            context.UserBlocks.Add(userBlock);

            // Add message reactions by user on others' messages
            var chatroom = new Chatroom
            {
                ChatroomId = 1,
                Name = "Test Chat",
                CreatedByUserId = "user456"
            };
            var message = new Message
            {
                MessageId = 1,
                ChatroomId = 1,
                UserId = "user456",
                Content = "Test message"
            };
            var reaction = new MessageReaction
            {
                MessageId = 1,
                UserId = "user123",
                Emoji = "👍"
            };
            context.Chatrooms.Add(chatroom);
            context.Messages.Add(message);
            context.MessageReactions.Add(reaction);
            await context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByIdAsync("user123"))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var service = new UserService(context, _userManagerMock.Object);

            var result = await service.DeleteUserAsync("user123");

            Assert.True(result);
            _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
            
            // Verify blocks cleaned up
            var remainingBlocks = await context.UserBlocks
                .Where(ub => ub.BlockingUserId == "user123")
                .ToListAsync();
            Assert.Empty(remainingBlocks);

            // Verify reactions cleaned up
            var remainingReactions = await context.MessageReactions
                .Where(mr => mr.UserId == "user123")
                .ToListAsync();
            Assert.Empty(remainingReactions);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithNullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var service = new UserService(context, _userManagerMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdateUserProfileAsync(null!, "DisplayName", "Bio"));
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var service = new UserService(context, _userManagerMock.Object);

            _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UpdateUserProfileAsync("user123", "NewName", "New Bio"));
            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_SuccessfullyUpdatesProfile()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser
            {
                Id = "user123",
                UserName = "testuser",
                DisplayName = "Old Name",
                Bio = "Old Bio"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync("user123"))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var service = new UserService(context, _userManagerMock.Object);

            var result = await service.UpdateUserProfileAsync("user123", "New Name", "New Bio");

            Assert.True(result);
            Assert.Equal("New Name", user.DisplayName);
            Assert.Equal("New Bio", user.Bio);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsUser()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser { Id = "user123", UserName = "testuser" };

            _userManagerMock.Setup(x => x.FindByIdAsync("user123"))
                .ReturnsAsync(user);

            var service = new UserService(context, _userManagerMock.Object);

            var result = await service.GetUserByIdAsync("user123");

            Assert.NotNull(result);
            Assert.Equal("user123", result.Id);
            Assert.Equal("testuser", result.UserName);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ReturnsUser()
        {
            var context = DbContextHelper.CreateInMemoryContext(Guid.NewGuid().ToString());
            var user = new ApplicationUser { Id = "user123", UserName = "testuser" };

            _userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
                .ReturnsAsync(user);

            var service = new UserService(context, _userManagerMock.Object);

            var result = await service.GetUserByUsernameAsync("testuser");

            Assert.NotNull(result);
            Assert.Equal("user123", result.Id);
            Assert.Equal("testuser", result.UserName);
        }
    }
}
