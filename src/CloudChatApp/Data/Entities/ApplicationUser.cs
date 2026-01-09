using Microsoft.AspNetCore.Identity;

namespace CloudChatApp.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<ChatroomMember> ChatroomMemberships { get; set; } = new List<ChatroomMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
        public ICollection<UserBlock> BlockingUsers { get; set; } = new List<UserBlock>();
    }
}
