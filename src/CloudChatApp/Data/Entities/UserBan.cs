using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Data.Entities
{
    [Index(nameof(ChatroomId))]
    [Index(nameof(BannedUserId))]
    [Index(nameof(ChatroomId), nameof(BannedUserId), IsUnique = true)]
    public class UserBan
    {
        [Key]
        public int BanId { get; set; }

        public int ChatroomId { get; set; }
        public Chatroom Chatroom { get; set; } = null!;

        public string BannedUserId { get; set; } = null!;
        public ApplicationUser BannedUser { get; set; } = null!;

        public string? BannedByUserId { get; set; }
        public ApplicationUser? BannedByUser { get; set; }

        public string? Reason { get; set; }

        public DateTime BannedAt { get; set; } = DateTime.UtcNow;
    }
}
