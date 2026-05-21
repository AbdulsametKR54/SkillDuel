using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Questions");
        }
    }
}
