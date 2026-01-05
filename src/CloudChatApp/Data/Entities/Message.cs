namespace CloudChatApp.Data.Entities
{
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(ChatroomId))]
    [Index(nameof(UserId))]
    [Index(nameof(ReplyToMessageId))]
    public class Message
    {
        public int MessageId { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int ChatroomId { get; set; }
        public Chatroom Chatroom { get; set; } = null!;

        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Reply threading
        public int? ReplyToMessageId { get; set; }
        public Message? ReplyToMessage { get; set; }

        // Navigation
        public ICollection<Message> Replies { get; set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}