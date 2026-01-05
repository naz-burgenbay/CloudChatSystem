using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Data.Entities
{
    [Index(nameof(MessageId), nameof(UserId), nameof(Emoji), IsUnique = true)]
    public class MessageReaction
    {
        public int MessageReactionId { get; set; }

        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public string Emoji { get; set; } = null!;
        public DateTime ReactedAt { get; set; } = DateTime.UtcNow;
    }
}
