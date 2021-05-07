using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class AddTimeToAchievementsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TimeBetaInvolver",
                table: "Achievements",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeProfessional",
                table: "Achievements",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeBetaInvolver",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "TimeProfessional",
                table: "Achievements");
        }
    }
}
