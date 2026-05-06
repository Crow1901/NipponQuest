using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpacedRepetition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewed",
                table: "Flashcards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessCount",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReviewed",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "SuccessCount",
                table: "Flashcards");
        }
    }
}
