using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCategoryStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCategoryStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectAnswersCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswersCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategoryStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCategoryStats_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCategoryStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryStats_CategoryId",
                table: "UserCategoryStats",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryStats_UserId",
                table: "UserCategoryStats",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCategoryStats");
        }
    }
}
