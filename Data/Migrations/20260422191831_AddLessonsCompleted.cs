using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonsCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonsCompleted",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LessonsCompleted",
                table: "AspNetUsers");
        }
    }
}
