using CloudChatApp.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Data.Entities
{
    [Index(nameof(UserId))]
    [Index(nameof(ChatroomId))]
    [Index(nameof(ChatroomId), nameof(UserId), IsUnique = true)]
    public class ChatroomMember
    {
        public int ChatroomMemberId { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public int ChatroomId { get; set; }
        public Chatroom Chatroom { get; set; } = null!;
        public ChatroomSystemRole SystemRole { get; set; } = ChatroomSystemRole.Member;

        public int? ChatroomRoleId { get; set; }
        public ChatroomRole? Role { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsMuted { get; set; } = false;
    }
}