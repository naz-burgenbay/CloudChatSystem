using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Data.Entities
{
    [Index(nameof(BlockingUserId), nameof(BlockedUserId), IsUnique = true)]
    public class UserBlock
    {
        public int UserBlockId { get; set; }

        public string BlockingUserId { get; set; } = null!;
        public ApplicationUser BlockingUser { get; set; } = null!;

        public string BlockedUserId { get; set; } = null!;
        public ApplicationUser BlockedUser { get; set; } = null!;

        public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
    }
}
