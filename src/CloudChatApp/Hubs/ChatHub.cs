using CloudChatApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CloudChatApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IChatroomMemberService _chatroomMemberService;

        public ChatHub(IMessageService messageService, IChatroomMemberService chatroomMemberService)
        {
            _messageService = messageService;
            _chatroomMemberService = chatroomMemberService;
        }

        public async Task JoinChatroom(int chatroomId)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            try
            {
                // Verify user is a member
                var isMember = await _chatroomMemberService.IsMemberAsync(chatroomId, userId);
                if (!isMember)
                {
                    await Clients.Caller.SendAsync("Error", "User is not a member of this chatroom.");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"chatroom_{chatroomId}");
                await Clients.Group($"chatroom_{chatroomId}").SendAsync("UserJoined", userId, chatroomId);
            }
            catch (ArgumentException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task LeaveChatroom(int chatroomId)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chatroom_{chatroomId}");
            await Clients.Group($"chatroom_{chatroomId}").SendAsync("UserLeft", userId, chatroomId);
        }

        public async Task SendMessage(int chatroomId, string content, int? replyToMessageId = null)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            try
            {
                var message = await _messageService.SendMessageAsync(chatroomId, userId, content, replyToMessageId);

                // Broadcast to all chatroom members
                await Clients.Group($"chatroom_{chatroomId}").SendAsync("ReceiveMessage", new
                {
                    message.MessageId,
                    message.ChatroomId,
                    message.UserId,
                    message.Content,
                    message.ReplyToMessageId,
                    message.SentAt,
                    message.EditedAt
                });
            }
            catch (ArgumentException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task EditMessage(int messageId, string newContent)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            try
            {
                var result = await _messageService.EditMessageAsync(messageId, userId, newContent);
                if (result)
                {
                    var message = await _messageService.GetMessageByIdAsync(messageId);
                    if (message != null)
                    {
                        await Clients.Group($"chatroom_{message.ChatroomId}").SendAsync("MessageEdited", new
                        {
                            message.MessageId,
                            message.Content,
                            message.EditedAt
                        });
                    }
                }
            }
            catch (ArgumentException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task DeleteMessage(int messageId)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            try
            {
                var message = await _messageService.GetMessageByIdAsync(messageId);
                if (message != null)
                {
                    var chatroomId = message.ChatroomId;
                    var result = await _messageService.DeleteMessageAsync(messageId, userId);
                    if (result)
                    {
                        await Clients.Group($"chatroom_{chatroomId}").SendAsync("MessageDeleted", messageId);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task SendTypingIndicator(int chatroomId)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID cannot be null or empty.");
                return;
            }

            await Clients.OthersInGroup($"chatroom_{chatroomId}").SendAsync("UserTyping", userId, chatroomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
