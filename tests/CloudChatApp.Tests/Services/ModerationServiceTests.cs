using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class ModerationServiceTests
    {
        [Fact]
        public async Task MuteMemberAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ModerationService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.MuteMemberAsync(0, "user2", "user1"));
        }

        [Fact]
        public async Task MuteMemberAsync_NullTargetUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ModerationService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.MuteMemberAsync(1, null!, "user1"));
        }

        [Fact]
        public async Task MuteMemberAsync_NullActionUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ModerationService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.MuteMemberAsync(1, "user2", null!));
        }

        [Fact]
        public async Task MuteMemberAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MuteMemberAsync(999, "user2", "user1"));
        }

        [Fact]
        public async Task MuteMemberAsync_UserWithoutPermission_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3", Email = "user3@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2, user3);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member2 = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow };
            var member3 = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user3", JoinedAt = DateTime.UtcNow };
            context.ChatroomMembers.AddRange(member2, member3);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MuteMemberAsync(chatroom.ChatroomId, "user3", "user2"));
        }

        [Fact]
        public async Task MuteMemberAsync_MemberNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MuteMemberAsync(chatroom.ChatroomId, "user2", "user1"));
        }

        [Fact]
        public async Task MuteMemberAsync_AlreadyMuted_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow, IsMuted = true };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MuteMemberAsync(chatroom.ChatroomId, "user2", "user1"));
        }

        [Fact]
        public async Task MuteMemberAsync_OwnerMutesMember_MutesSuccessfully()
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

            var service = new ModerationService(context);

            var result = await service.MuteMemberAsync(chatroom.ChatroomId, "user2", "user1");

            Assert.True(result);
            var mutedMember = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == "user2");
            Assert.NotNull(mutedMember);
            Assert.True(mutedMember.IsMuted);
        }

        [Fact]
        public async Task UnmuteMemberAsync_NotMuted_ThrowsInvalidOperationException()
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

            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UnmuteMemberAsync(chatroom.ChatroomId, "user2", "user1"));
        }

        [Fact]
        public async Task UnmuteMemberAsync_ValidUnmute_UnmutesSuccessfully()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user2", JoinedAt = DateTime.UtcNow, IsMuted = true };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            var result = await service.UnmuteMemberAsync(chatroom.ChatroomId, "user2", "user1");

            Assert.True(result);
            var unmutedMember = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == "user2");
            Assert.NotNull(unmutedMember);
            Assert.False(unmutedMember.IsMuted);
        }

        [Fact]
        public async Task IsMutedAsync_MutedUser_ReturnsTrue()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow, IsMuted = true };
            context.ChatroomMembers.Add(member);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            var result = await service.IsMutedAsync(chatroom.ChatroomId, "user1");

            Assert.True(result);
        }

        [Fact]
        public async Task IsMutedAsync_NotMutedUser_ReturnsFalse()
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

            var service = new ModerationService(context);

            var result = await service.IsMutedAsync(chatroom.ChatroomId, "user1");

            Assert.False(result);
        }

        [Fact]
        public async Task BanUserAsync_CannotBanOwner_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BanUserAsync(chatroom.ChatroomId, "user1", "user2", "Test reason"));
        }

        [Fact]
        public async Task BanUserAsync_OwnerBansUser_BansSuccessfully()
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

            var service = new ModerationService(context);

            var result = await service.BanUserAsync(chatroom.ChatroomId, "user2", "user1", "Test reason");

            Assert.True(result);
            var ban = await context.UserBans.FirstOrDefaultAsync(ub => ub.ChatroomId == chatroom.ChatroomId && ub.BannedUserId == "user2");
            Assert.NotNull(ban);
            Assert.Equal("Test reason", ban.Reason);
            var removedMember = await context.ChatroomMembers.FirstOrDefaultAsync(cm => cm.ChatroomId == chatroom.ChatroomId && cm.UserId == "user2");
            Assert.Null(removedMember);
        }

        [Fact]
        public async Task UnbanUserAsync_ValidUnban_RemovesBan()
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

            var service = new ModerationService(context);

            var result = await service.UnbanUserAsync(chatroom.ChatroomId, "user2", "user1");

            Assert.True(result);
            var removedBan = await context.UserBans.FirstOrDefaultAsync(ub => ub.ChatroomId == chatroom.ChatroomId && ub.BannedUserId == "user2");
            Assert.Null(removedBan);
        }

        [Fact]
        public async Task IsUserBannedAsync_BannedUser_ReturnsTrue()
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

            var service = new ModerationService(context);

            var result = await service.IsUserBannedAsync(chatroom.ChatroomId, "user2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsUserBannedAsync_NotBannedUser_ReturnsFalse()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ModerationService(context);

            var result = await service.IsUserBannedAsync(1, "user1");

            Assert.False(result);
        }

        [Fact]
        public async Task GetChatroomBansAsync_ReturnsAllBans()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "user3", Email = "user3@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2, user3);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var ban1 = new UserBan { ChatroomId = chatroom.ChatroomId, BannedUserId = "user2", BannedByUserId = "user1", BannedAt = DateTime.UtcNow };
            var ban2 = new UserBan { ChatroomId = chatroom.ChatroomId, BannedUserId = "user3", BannedByUserId = "user1", BannedAt = DateTime.UtcNow };
            context.UserBans.AddRange(ban1, ban2);
            await context.SaveChangesAsync();

            var service = new ModerationService(context);

            var result = await service.GetChatroomBansAsync(chatroom.ChatroomId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, b => b.BannedUserId == "user2");
            Assert.Contains(result, b => b.BannedUserId == "user3");
        }
    }
}
