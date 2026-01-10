using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudChatApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBanAndModerationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatroomMembers_ChatroomRoles_ChatroomRoleId",
                table: "ChatroomMembers");

            migrationBuilder.DropIndex(
                name: "IX_ChatroomMembers_ChatroomRoleId",
                table: "ChatroomMembers");

            migrationBuilder.DropColumn(
                name: "ChatroomRoleId",
                table: "ChatroomMembers");

            migrationBuilder.CreateTable(
                name: "ChatroomMemberRoles",
                columns: table => new
                {
                    ChatroomMemberId = table.Column<int>(type: "int", nullable: false),
                    ChatroomRoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatroomMemberRoles", x => new { x.ChatroomMemberId, x.ChatroomRoleId });
                    table.ForeignKey(
                        name: "FK_ChatroomMemberRoles_ChatroomMembers_ChatroomMemberId",
                        column: x => x.ChatroomMemberId,
                        principalTable: "ChatroomMembers",
                        principalColumn: "ChatroomMemberId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatroomMemberRoles_ChatroomRoles_ChatroomRoleId",
                        column: x => x.ChatroomRoleId,
                        principalTable: "ChatroomRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    BanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatroomId = table.Column<int>(type: "int", nullable: false),
                    BannedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BannedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.BanId);
                    table.ForeignKey(
                        name: "FK_UserBans_AspNetUsers_BannedByUserId",
                        column: x => x.BannedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserBans_AspNetUsers_BannedUserId",
                        column: x => x.BannedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBans_Chatrooms_ChatroomId",
                        column: x => x.ChatroomId,
                        principalTable: "Chatrooms",
                        principalColumn: "ChatroomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatroomMemberRoles_ChatroomRoleId",
                table: "ChatroomMemberRoles",
                column: "ChatroomRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_BannedByUserId",
                table: "UserBans",
                column: "BannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_BannedUserId",
                table: "UserBans",
                column: "BannedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ChatroomId",
                table: "UserBans",
                column: "ChatroomId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ChatroomId_BannedUserId",
                table: "UserBans",
                columns: new[] { "ChatroomId", "BannedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatroomMemberRoles");

            migrationBuilder.DropTable(
                name: "UserBans");

            migrationBuilder.AddColumn<int>(
                name: "ChatroomRoleId",
                table: "ChatroomMembers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatroomMembers_ChatroomRoleId",
                table: "ChatroomMembers",
                column: "ChatroomRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatroomMembers_ChatroomRoles_ChatroomRoleId",
                table: "ChatroomMembers",
                column: "ChatroomRoleId",
                principalTable: "ChatroomRoles",
                principalColumn: "Id");
        }
    }
}
