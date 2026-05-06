using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipponQuest.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlitzPersonalBest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlitzPersonalBests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Alphabet = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BestCorrect = table.Column<int>(type: "int", nullable: false),
                    BestPoints = table.Column<int>(type: "int", nullable: false),
                    BestCombo = table.Column<int>(type: "int", nullable: false),
                    BestAccuracy = table.Column<double>(type: "float", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlitzPersonalBests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlitzPersonalBests_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlitzPersonalBests_ApplicationUserId_Difficulty_Alphabet",
                table: "BlitzPersonalBests",
                columns: new[] { "ApplicationUserId", "Difficulty", "Alphabet" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlitzPersonalBests");
        }
    }
}
