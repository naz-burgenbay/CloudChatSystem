using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CloudChatApp.Data.Entities;

namespace CloudChatApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Chatroom> Chatrooms { get; set; }
        public DbSet<ChatroomMember> ChatroomMembers { get; set; }
        public DbSet<ChatroomRole> ChatroomRoles { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<UserBlock> UserBlocks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserBlock - restrict to prevent multiple cascade paths (handle block cleanup in service)
            modelBuilder.Entity<UserBlock>()
                .HasOne(ub => ub.BlockingUser)
                .WithMany(u => u.BlockingUsers)
                .HasForeignKey(ub => ub.BlockingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserBlock - if blocked user deleted, remove their blocks
            modelBuilder.Entity<UserBlock>()
                .HasOne(ub => ub.BlockedUser)
                .WithMany(u => u.BlockedUsers)
                .HasForeignKey(ub => ub.BlockedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - if chatroom deleted, remove its messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chatroom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatroomMember - if chatroom deleted, remove memberships
            modelBuilder.Entity<ChatroomMember>()
                .HasOne(cm => cm.Chatroom)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade);

            // MessageReaction - if message deleted, remove reactions
            modelBuilder.Entity<MessageReaction>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // MessageReaction - restrict to prevent multiple cascade paths (handle user reaction cleanup in service)
            modelBuilder.Entity<MessageReaction>()
                .HasOne(mr => mr.User)
                .WithMany(u => u.MessageReactions)
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatroomRole - if chatroom deleted, remove custom roles
            modelBuilder.Entity<ChatroomRole>()
                .HasOne(cr => cr.Chatroom)
                .WithMany(c => c.ChatroomRoles)
                .HasForeignKey(cr => cr.ChatroomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message reply - prevent cycles, handle in service
            modelBuilder.Entity<Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.NoAction);

            // Message - if user deleted, remove their messages
            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatroomMember - if user deleted, remove their memberships
            modelBuilder.Entity<ChatroomMember>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.ChatroomMemberships)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Chatroom creator - restrict deletion (ownership transfer handled in service)
            modelBuilder.Entity<Chatroom>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
