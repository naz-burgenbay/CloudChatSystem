using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class MessageServiceTests
    {
        [Fact]
        public async Task GetMessageByIdAsync_InvalidId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetMessageByIdAsync(0));
        }

        [Fact]
        public async Task GetMessageByIdAsync_MessageNotFound_ReturnsNull()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            var result = await service.GetMessageByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetMessageByIdAsync_ValidId_ReturnsMessage()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test message", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var result = await service.GetMessageByIdAsync(message.MessageId);

            Assert.NotNull(result);
            Assert.Equal("Test message", result.Content);
        }

        [Fact]
        public async Task SendMessageAsync_InvalidChatroomId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync(0, "user1", "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_NullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync(1, null!, "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_EmptyContent_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync(1, "user1", ""));
        }

        [Fact]
        public async Task SendMessageAsync_ChatroomNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessageAsync(999, "user1", "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessageAsync(chatroom.ChatroomId, "nonexistent", "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_UserNotMember_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessageAsync(chatroom.ChatroomId, "user2", "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_UserMuted_ThrowsInvalidOperationException()
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

            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessageAsync(chatroom.ChatroomId, "user1", "Test"));
        }

        [Fact]
        public async Task SendMessageAsync_ValidMessage_SendsSuccessfully()
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

            var service = new MessageService(context);

            var result = await service.SendMessageAsync(chatroom.ChatroomId, "user1", "Test message");

            Assert.NotNull(result);
            Assert.Equal("Test message", result.Content);
            Assert.Equal(chatroom.ChatroomId, result.ChatroomId);
            Assert.Equal("user1", result.UserId);
        }

        [Fact]
        public async Task EditMessageAsync_InvalidMessageId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.EditMessageAsync(0, "user1", "New content"));
        }

        [Fact]
        public async Task EditMessageAsync_MessageNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.EditMessageAsync(999, "user1", "New content"));
        }

        [Fact]
        public async Task EditMessageAsync_NotMessageOwner_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.EditMessageAsync(message.MessageId, "user2", "New content"));
        }

        [Fact]
        public async Task EditMessageAsync_ValidEdit_UpdatesMessage()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Old content", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var result = await service.EditMessageAsync(message.MessageId, "user1", "New content");

            Assert.True(result);
            var updated = await context.Messages.FirstOrDefaultAsync(m => m.MessageId == message.MessageId);
            Assert.NotNull(updated);
            Assert.Equal("New content", updated.Content);
            Assert.NotNull(updated.EditedAt);
        }

        [Fact]
        public async Task DeleteMessageAsync_InvalidMessageId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteMessageAsync(0, "user1"));
        }

        [Fact]
        public async Task DeleteMessageAsync_MessageNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteMessageAsync(999, "user1"));
        }

        [Fact]
        public async Task DeleteMessageAsync_ValidDeletion_DeletesMessage()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var result = await service.DeleteMessageAsync(message.MessageId, "user1");

            Assert.True(result);
            var deleted = await context.Messages.FirstOrDefaultAsync(m => m.MessageId == message.MessageId);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task GetChatroomMessagesAsync_ReturnsMessages()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message1 = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Message 1", SentAt = DateTime.UtcNow };
            var message2 = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Message 2", SentAt = DateTime.UtcNow.AddMinutes(1) };
            context.Messages.AddRange(message1, message2);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var result = await service.GetChatroomMessagesAsync(chatroom.ChatroomId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, m => m.Content == "Message 1");
            Assert.Contains(result, m => m.Content == "Message 2");
        }

        [Fact]
        public async Task SendMessageAsync_WithReply_CreatesReplyChain()
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

            var originalMessage = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Original", SentAt = DateTime.UtcNow };
            context.Messages.Add(originalMessage);
            await context.SaveChangesAsync();

            var service = new MessageService(context);

            var reply = await service.SendMessageAsync(chatroom.ChatroomId, "user1", "Reply", originalMessage.MessageId);

            Assert.NotNull(reply);
            Assert.Equal(originalMessage.MessageId, reply.ReplyToMessageId);
        }
    }
}
