namespace CloudChatApp.Data.Entities
{
    public class ChatroomMemberRole
    {
        public int ChatroomMemberId { get; set; }
        public ChatroomMember ChatroomMember { get; set; } = null!;

        public int ChatroomRoleId { get; set; }
        public ChatroomRole ChatroomRole { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
