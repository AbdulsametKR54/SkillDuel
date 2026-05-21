using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IsBanned was already added manually or in another migration
            // migrationBuilder.AddColumn<bool>(
            //     name: "IsBanned",
            //     table: "Users",
            //     type: "boolean",
            //     nullable: false,
            //     defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Code",
                table: "Rooms",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Status",
                table: "Rooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_DifficultyLevel",
                table: "Questions",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Status",
                table: "Questions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Status",
                table: "GameSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserId",
                table: "Friendships",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_Code",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Status",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Questions_DifficultyLevel",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Status",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_Status",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_UserId",
                table: "Friendships");

            // migrationBuilder.DropColumn(
            //     name: "IsBanned",
            //     table: "Users");
        }
    }
}
