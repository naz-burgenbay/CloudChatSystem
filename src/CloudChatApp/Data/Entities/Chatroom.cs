namespace CloudChatApp.Data.Entities
{
    public class Chatroom
    {
        public int ChatroomId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public string CreatedByUserId { get; set; } = null!;
        public ApplicationUser CreatedByUser { get; set; } = null!;

        // Navigation
        public ICollection<ChatroomMember> Members { get; set; } = new List<ChatroomMember>();
        public ICollection<ChatroomRole> ChatroomRoles { get; set; } = new List<ChatroomRole>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<UserBan> UserBans { get; set; } = new List<UserBan>();
    }
}