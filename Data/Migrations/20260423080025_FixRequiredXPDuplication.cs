using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRequiredXPDuplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyStreak",
                table: "AspNetUsers",
                newName: "LastWeekGlobalRank");

            migrationBuilder.AddColumn<int>(
                name: "LastWeekArenaRank",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWeekArenaRank",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "LastWeekGlobalRank",
                table: "AspNetUsers",
                newName: "DailyStreak");
        }
    }
}
