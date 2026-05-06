using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Decks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dislikes",
                table: "Decks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Downloads",
                table: "Decks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Decks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Likes",
                table: "Decks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "Dislikes",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "Downloads",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "Likes",
                table: "Decks");
        }
    }
}
