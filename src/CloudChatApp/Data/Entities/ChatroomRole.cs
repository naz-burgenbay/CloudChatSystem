namespace CloudChatApp.Data.Entities
{
    public class ChatroomRole
    {
        public int Id { get; set; }
        public int ChatroomId { get; set; }
        public Chatroom Chatroom { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool CanDeleteMessages { get; set; }
        public bool CanBanUsers { get; set; }
        public bool CanManageRoles { get; set; }

        // Navigation
        public ICollection<ChatroomMemberRole> MemberAssignments { get; set; } = new List<ChatroomMemberRole>();
    }
}