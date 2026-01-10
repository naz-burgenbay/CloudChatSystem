using CloudChatApp.Data;
using CloudChatApp.Data.Entities;
using CloudChatApp.Services;
using CloudChatApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Services
{
    public class MessageReactionServiceTests
    {
        [Fact]
        public async Task AddReactionAsync_InvalidMessageId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddReactionAsync(0, "user1", "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_NullUserId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddReactionAsync(1, null!, "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_EmptyEmoji_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddReactionAsync(1, "user1", ""));
        }

        [Fact]
        public async Task AddReactionAsync_MessageNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddReactionAsync(999, "user1", "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_UserNotFound_ThrowsInvalidOperationException()
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

            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddReactionAsync(message.MessageId, "nonexistent", "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_UserNotMemberOfChatroom_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddReactionAsync(message.MessageId, "user2", "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_DuplicateReaction_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var reaction = new MessageReaction { MessageId = message.MessageId, UserId = "user1", Emoji = "👍", ReactedAt = DateTime.UtcNow };
            context.MessageReactions.Add(reaction);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddReactionAsync(message.MessageId, "user1", "👍"));
        }

        [Fact]
        public async Task AddReactionAsync_ValidReaction_ReturnsReaction()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.Add(user1);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var member = new ChatroomMember { ChatroomId = chatroom.ChatroomId, UserId = "user1", JoinedAt = DateTime.UtcNow };
            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.ChatroomMembers.Add(member);
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            var result = await service.AddReactionAsync(message.MessageId, "user1", "👍");

            Assert.NotNull(result);
            Assert.Equal(message.MessageId, result.MessageId);
            Assert.Equal("user1", result.UserId);
            Assert.Equal("👍", result.Emoji);
        }

        [Fact]
        public async Task RemoveReactionAsync_InvalidMessageId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveReactionAsync(0, "user1", "👍"));
        }

        [Fact]
        public async Task RemoveReactionAsync_ReactionNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveReactionAsync(1, "user1", "👍"));
        }

        [Fact]
        public async Task RemoveReactionAsync_ValidRemoval_ReturnsTrue()
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

            var reaction = new MessageReaction { MessageId = message.MessageId, UserId = "user1", Emoji = "👍", ReactedAt = DateTime.UtcNow };
            context.MessageReactions.Add(reaction);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            var result = await service.RemoveReactionAsync(message.MessageId, "user1", "👍");

            Assert.True(result);
            var removedReaction = await context.MessageReactions.FirstOrDefaultAsync(mr => mr.MessageId == message.MessageId && mr.UserId == "user1");
            Assert.Null(removedReaction);
        }

        [Fact]
        public async Task GetReactionsAsync_InvalidMessageId_ThrowsArgumentException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetReactionsAsync(0));
        }

        [Fact]
        public async Task GetReactionsAsync_MessageNotFound_ThrowsInvalidOperationException()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var service = new MessageReactionService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetReactionsAsync(999));
        }

        [Fact]
        public async Task GetReactionsAsync_ReturnsAllReactions()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var reaction1 = new MessageReaction { MessageId = message.MessageId, UserId = "user1", Emoji = "👍", ReactedAt = DateTime.UtcNow };
            var reaction2 = new MessageReaction { MessageId = message.MessageId, UserId = "user2", Emoji = "❤️", ReactedAt = DateTime.UtcNow };
            context.MessageReactions.AddRange(reaction1, reaction2);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            var result = await service.GetReactionsAsync(message.MessageId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.UserId == "user1" && r.Emoji == "👍");
            Assert.Contains(result, r => r.UserId == "user2" && r.Emoji == "❤️");
        }

        [Fact]
        public async Task GetReactionCountAsync_ReturnsCorrectCount()
        {
            var context = DbContextHelper.CreateInMemoryContext();
            var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com" };
            var chatroom = new Chatroom { Name = "Test", CreatedByUserId = "user1", CreatedAt = DateTime.UtcNow };
            context.Users.AddRange(user1, user2);
            context.Chatrooms.Add(chatroom);
            await context.SaveChangesAsync();

            var message = new Message { ChatroomId = chatroom.ChatroomId, UserId = "user1", Content = "Test", SentAt = DateTime.UtcNow };
            context.Messages.Add(message);
            await context.SaveChangesAsync();

            var reaction1 = new MessageReaction { MessageId = message.MessageId, UserId = "user1", Emoji = "👍", ReactedAt = DateTime.UtcNow };
            var reaction2 = new MessageReaction { MessageId = message.MessageId, UserId = "user2", Emoji = "👍", ReactedAt = DateTime.UtcNow };
            context.MessageReactions.AddRange(reaction1, reaction2);
            await context.SaveChangesAsync();

            var service = new MessageReactionService(context);

            var result = await service.GetReactionCountAsync(message.MessageId, "👍");

            Assert.Equal(2, result);
        }
    }
}
