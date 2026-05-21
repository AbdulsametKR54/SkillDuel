using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSessionPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Player3EloChange",
                table: "GameSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Player3Id",
                table: "GameSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player3Score",
                table: "GameSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player4EloChange",
                table: "GameSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Player4Id",
                table: "GameSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Player4Score",
                table: "GameSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Player3Id",
                table: "GameSessions",
                column: "Player3Id");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Player4Id",
                table: "GameSessions",
                column: "Player4Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Users_Player3Id",
                table: "GameSessions",
                column: "Player3Id",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Users_Player4Id",
                table: "GameSessions",
                column: "Player4Id",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Users_Player3Id",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Users_Player4Id",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_Player3Id",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_Player4Id",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player3EloChange",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player3Id",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player3Score",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player4EloChange",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player4Id",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Player4Score",
                table: "GameSessions");
        }
    }
}
