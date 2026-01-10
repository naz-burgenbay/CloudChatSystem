using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class ChatroomMemberServiceTests
    {
        [Fact]
        public async Task GetMemberAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetMemberAsync(0, "user1"));
        }

        [Fact]
        public async Task GetMemberAsync_NullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetMemberAsync(1, null!));
        }

        [Fact]
        public async Task GetMemberAsync_MemberNotFound_ReturnsNull()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            var result = await service.GetMemberAsync(1, "user1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetMemberAsync_ValidMember_ReturnsMember()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            var result = await service.GetMemberAsync(chatroom.ChatroomId, "user1");

            Assert.NotNull(result);
            Assert.Equal("user1", result.UserId);
        }

        [Fact]
        public async Task AddMemberAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddMemberAsync(0, "user1"));
        }

        [Fact]
        public async Task AddMemberAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync(999, "user1"));
        }

        [Fact]
        public async Task AddMemberAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync(chatroom.ChatroomId, "nonexistent"));
        }

        [Fact]
        public async Task AddMemberAsync_UserBanned_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var ban = new UserBan { ChatroomId = chatroom.ChatroomId, BannedUserId = "user2", BannedByUserId = "user1", BannedAt = DateTime.UtcNow };
            context.UserBans.Add(ban);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync(chatroom.ChatroomId, "user2"));
        }

        [Fact]
        public async Task AddMemberAsync_AlreadyMember_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync(chatroom.ChatroomId, "user1"));
        }

        [Fact]
        public async Task AddMemberAsync_ValidMember_AddsSuccessfully()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            var result = await service.AddMemberAsync(chatroom.ChatroomId, "user2");

            Assert.True(result);
            var member = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == "user2");
            Assert.NotNull(member);
        }

        [Fact]
        public async Task RemoveMemberAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveMemberAsync(0, "user1"));
        }

        [Fact]
        public async Task RemoveMemberAsync_MemberNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveMemberAsync(1, "user1"));
        }

        [Fact]
        public async Task RemoveMemberAsync_OwnerCannotLeave_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveMemberAsync(chatroom.ChatroomId, "user1"));
        }

        [Fact]
        public async Task RemoveMemberAsync_ValidRemoval_RemovesMember()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            var result = await service.RemoveMemberAsync(chatroom.ChatroomId, "user2");

            Assert.True(result);
            var removed = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == "user2");
            Assert.Null(removed);
        }

        [Fact]
        public async Task GetChatroomMembersAsync_ReturnsAllMembers()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member1 = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            var member2 = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.AddRange(member1, member2);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            var result = await service.GetChatroomMembersAsync(chatroom.ChatroomId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, m => m.UserId == "user1");
            Assert.Contains(result, m => m.UserId == "user2");
        }

        [Fact]
        public async Task IsMemberAsync_MemberExists_ReturnsTrue()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ChatroomMemberService(context);

            var result = await service.IsMemberAsync(chatroom.ChatroomId, "user1");

            Assert.True(result);
        }

        [Fact]
        public async Task IsMemberAsync_MemberDoesNotExist_ReturnsFalse()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomMemberService(context);

            var result = await service.IsMemberAsync(1, "user1");

            Assert.False(result);
        }
    }
}
