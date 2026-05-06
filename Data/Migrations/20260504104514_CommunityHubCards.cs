using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class CommunityHubCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommunityClone",
                table: "Decks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentDeckId",
                table: "Decks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Decks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommunityClone",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "ParentDeckId",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Decks");
        }
    }
}
