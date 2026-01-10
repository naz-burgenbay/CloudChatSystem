using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class ChatroomServiceTests
    {
        [Fact]
        public async Task GetChatroomByIdAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetChatroomByIdAsync(0));
        }

        [Fact]
        public async Task GetChatroomByIdAsync_ChatroomNotFound_ReturnsNull()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            var result = await service.GetChatroomByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetChatroomByIdAsync_ValidId_ReturnsChatroom()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var chatroom = new Chatroom { Name = "Test Chatroom", Description = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.GetChatroomByIdAsync(chatroom.ChatroomId);

            Assert.NotNull(result);
            Assert.Equal("Test Chatroom", result.Name);
        }

        [Fact]
        public async Task CreateChatroomAsync_NullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateChatroomAsync(null!, "Test", "Description"));
        }

        [Fact]
        public async Task CreateChatroomAsync_EmptyName_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateChatroomAsync("user1", "", "Description"));
        }

        [Fact]
        public async Task CreateChatroomAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateChatroomAsync("nonexistent", "Test", "Description"));
        }

        [Fact]
        public async Task CreateChatroomAsync_ValidChatroom_CreatesChatroomAndAddsMember()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.CreateChatroomAsync("user1", "Test Chatroom", "Test Description");

            Assert.NotNull(result);
            Assert.Equal("Test Chatroom", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("user1", result.CreatedByUserId);

            var member = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == result.ChatroomId && cm.UserId == "user1");
            Assert.NotNull(member);
        }

        [Fact]
        public async Task DeleteChatroomAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteChatroomAsync(0));
        }

        [Fact]
        public async Task DeleteChatroomAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteChatroomAsync(999));
        }

        [Fact]
        public async Task DeleteChatroomAsync_ValidId_DeletesChatroom()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.DeleteChatroomAsync(chatroom.ChatroomId);

            Assert.True(result);
            var deleted = await context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroom.ChatroomId);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task UpdateChatroomAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateChatroomAsync(0, "NewName", "NewDescription"));
        }

        [Fact]
        public async Task UpdateChatroomAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateChatroomAsync(999, "NewName", "NewDescription"));
        }

        [Fact]
        public async Task UpdateChatroomAsync_ValidUpdate_UpdatesChatroom()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var chatroom = new Chatroom { Name = "Old Name", Description = "Old Description", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.UpdateChatroomAsync(chatroom.ChatroomId, "New Name", "New Description");

            Assert.True(result);
            var updated = await context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroom.ChatroomId);
            Assert.NotNull(updated);
            Assert.Equal("New Name", updated.Name);
            Assert.Equal("New Description", updated.Description);
        }

        [Fact]
        public async Task TransferOwnershipAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.TransferOwnershipAsync(0, "user2"));
        }

        [Fact]
        public async Task TransferOwnershipAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferOwnershipAsync(999, "user2"));
        }

        [Fact]
        public async Task TransferOwnershipAsync_NewOwnerNotMember_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferOwnershipAsync(chatroom.ChatroomId, "user2"));
        }

        [Fact]
        public async Task TransferOwnershipAsync_ValidTransfer_UpdatesOwner()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.TransferOwnershipAsync(chatroom.ChatroomId, "user2");

            Assert.True(result);
            var updated = await context.Chatrooms.FirstOrDefaultAsync(c => c.ChatroomId == chatroom.ChatroomId);
            Assert.NotNull(updated);
            Assert.Equal("user2", updated.CreatedByUserId);
        }

        [Fact]
        public async Task GetUserChatroomsAsync_ReturnsAllUserChatrooms()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            context.Users.Add(user1);
            await context.SaveChangesAsync();

            var chatroom1 = new Chatroom { Name = "Chatroom 1", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            var chatroom2 = new Chatroom { Name = "Chatroom 2", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Chatrooms.AddRange(chatroom1, chatroom2);
            await context.SaveChangesAsync();

            var member1 = new ChatroomMember { ChatroomId = chatroom1.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            var member2 = new ChatroomMember { ChatroomId = chatroom2.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.AddRange(member1, member2);
            await context.SaveChangesAsync();

            var service = new ChatroomService(context);

            var result = await service.GetUserChatroomsAsync("user1");

            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Chatroom 1");
            Assert.Contains(result, c => c.Name == "Chatroom 2");
        }
    }
}
