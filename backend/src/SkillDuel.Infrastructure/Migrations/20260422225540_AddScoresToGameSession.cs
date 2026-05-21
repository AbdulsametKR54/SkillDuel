using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoresToGameSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Player1EloChange",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Player1Score",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Player2EloChange",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Player2Score",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Users_Player1Id",
                table: "GameSessions",
                column: "Player1Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Users_Player2Id",
                table: "GameSessions",
                column: "Player2Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Users_Player1Id",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Users_Player2Id",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player1EloChange",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player1Score",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player2EloChange",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player2Score",
                table: "GameSessions");
        }
    }
}
