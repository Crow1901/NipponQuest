using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacedRepetitionLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageFilePath",
                table: "Flashcards",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AudioFilePath",
                table: "Flashcards",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<double>(
                name: "EaseFactor",
                table: "Flashcards",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Flashcards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReview",
                table: "Flashcards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EaseFactor",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Flashcards");

            migrationBuilder.DropColumn(
                name: "NextReview",
                table: "Flashcards");

            migrationBuilder.AlterColumn<string>(
                name: "ImageFilePath",
                table: "Flashcards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AudioFilePath",
                table: "Flashcards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
