using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class ChatroomRoleServiceTests
    {
        [Fact]
        public async Task GetRoleByIdAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetRoleByIdAsync(0));
        }

        [Fact]
        public async Task GetRoleByIdAsync_RoleNotFound_ReturnsNull()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            var result = await service.GetRoleByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetRoleByIdAsync_ValidId_ReturnsRole()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.GetRoleByIdAsync(role.Id);

            Assert.NotNull(result);
            Assert.Equal("Moderator", result.Name);
        }

        [Fact]
        public async Task CreateRoleAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRoleAsync(0, "user1", "Moderator", true, false, false));
        }

        [Fact]
        public async Task CreateRoleAsync_NullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRoleAsync(1, null!, "Moderator", true, false, false));
        }

        [Fact]
        public async Task CreateRoleAsync_EmptyRoleName_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateRoleAsync(1, "user1", "", true, false, false));
        }

        [Fact]
        public async Task CreateRoleAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateRoleAsync(999, "user1", "Moderator", true, false, false));
        }

        [Fact]
        public async Task CreateRoleAsync_UserWithoutPermission_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateRoleAsync(chatroom.ChatroomId, "user2", "Moderator", true, false, false));
        }

        [Fact]
        public async Task CreateRoleAsync_OwnerCreatesRole_CreatesSuccessfully()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.CreateRoleAsync(chatroom.ChatroomId, "user1", "Moderator", true, false, false);

            Assert.NotNull(result);
            Assert.Equal("Moderator", result.Name);
            Assert.True(result.CanDeleteMessages);
            Assert.False(result.CanBanUsers);
        }

        [Fact]
        public async Task UpdateRoleAsync_InvalidRoleId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateRoleAsync(0, "user1", "New Name", null, null, null));
        }

        [Fact]
        public async Task UpdateRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRoleAsync(999, "user1", "New Name", null, null, null));
        }

        [Fact]
        public async Task UpdateRoleAsync_UserWithoutPermission_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateRoleAsync(role.Id, "user2", "New Name", null, null, null));
        }

        [Fact]
        public async Task UpdateRoleAsync_ValidUpdate_UpdatesRole()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.UpdateRoleAsync(role.Id, "user1", "Admin", true, true, true);

            Assert.True(result);
            var updated = await context.ChatroomRoles.FirstOrDefaultAsync(r => r.Id == role.Id);
            Assert.NotNull(updated);
            Assert.Equal("Admin", updated.Name);
            Assert.True(updated.CanDeleteMessages);
            Assert.True(updated.CanBanUsers);
            Assert.True(updated.CanManageRoles);
        }

        [Fact]
        public async Task DeleteRoleAsync_InvalidRoleId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteRoleAsync(0, "user1"));
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new ChatroomRoleService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteRoleAsync(999, "user1"));
        }

        [Fact]
        public async Task DeleteRoleAsync_ValidDeletion_DeletesRole()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.DeleteRoleAsync(role.Id, "user1");

            Assert.True(result);
            var deleted = await context.ChatroomRoles.FirstOrDefaultAsync(r => r.Id == role.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task AssignRoleToMemberAsync_ValidAssignment_AssignsRole()
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

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.AssignRoleToMemberAsync(role.Id, chatroom.ChatroomId, "user2", "user1");

            Assert.True(result);
            var memberRole = await context.ChatroomMemberRoles.FirstOrDefaultAsync(cmr => cmr.ChatroomMemberId == member.ChatroomMemberId && cmr.ChatroomRoleId == role.Id);
            Assert.NotNull(memberRole);
        }

        [Fact]
        public async Task RemoveRoleFromMemberAsync_ValidRemoval_RemovesRole()
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

            var role = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            context.ChatroomRoles.Add(role);
            await context.SaveChangesAsync();

            var memberRole = new ChatroomMemberRole { ChatroomMemberId = member.ChatroomMemberId, ChatroomRoleId = role.Id };
            context.ChatroomMemberRoles.Add(memberRole);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.RemoveRoleFromMemberAsync(role.Id, chatroom.ChatroomId, "user2", "user1");

            Assert.True(result);
            var removed = await context.ChatroomMemberRoles.FirstOrDefaultAsync(cmr => cmr.ChatroomMemberId == member.ChatroomMemberId && cmr.ChatroomRoleId == role.Id);
            Assert.Null(removed);
        }

        [Fact]
        public async Task GetChatroomRolesAsync_ReturnsAllRoles()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var role1 = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Moderator", CanDeleteMessages = true, CanBanUsers = false, CanManageRoles = false };
            var role2 = new ChatroomRole { ChatroomId = chatroom.ChatroomId, Name = "Admin", CanDeleteMessages = true, CanBanUsers = true, CanManageRoles = true };
            context.ChatroomRoles.AddRange(role1, role2);
            await context.SaveChangesAsync();

            var service = new ChatroomRoleService(context);

            var result = await service.GetChatroomRolesAsync(chatroom.ChatroomId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.Name == "Moderator");
            Assert.Contains(result, r => r.Name == "Admin");
        }
    }
}
