using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AlterAchievement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_Profile_ProfileID",
                table: "Achievements");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "BetaInvolver",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "Professional",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "ProfileID",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "TimeBetaInvolver",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "TimeProfessional",
                table: "Achievements");

            migrationBuilder.RenameColumn(
                name: "AchievementsID",
                table: "Achievements",
                newName: "AchievementID");

            migrationBuilder.AlterColumn<int>(
                name: "SeqNo",
                table: "Profile",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Achievements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Achievements",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProfileAchievement",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AchievementID = table.Column<int>(type: "int", nullable: false),
                    AchieveDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileAchievement", x => new { x.ProfileID, x.AchievementID });
                    table.ForeignKey(
                        name: "FK_ProfileAchievement_Achievements_AchievementID",
                        column: x => x.AchievementID,
                        principalTable: "Achievements",
                        principalColumn: "AchievementID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileAchievement_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAchievement_AchievementID",
                table: "ProfileAchievement",
                column: "AchievementID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileAchievement");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Achievements");

            migrationBuilder.RenameColumn(
                name: "AchievementID",
                table: "Achievements",
                newName: "AchievementsID");

            migrationBuilder.AlterColumn<int>(
                name: "SeqNo",
                table: "Profile",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "BetaInvolver",
                table: "Achievements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Professional",
                table: "Achievements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileID",
                table: "Achievements",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeBetaInvolver",
                table: "Achievements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeProfessional",
                table: "Achievements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Achievements_Profile_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
