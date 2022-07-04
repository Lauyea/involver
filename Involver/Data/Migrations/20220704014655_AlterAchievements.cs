using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AlterAchievements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementID);
                });

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
                    table.PrimaryKey("PK_ProfileAchievement", x => new { x.ProfileID, x.AchievementID })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_ProfileAchievement_Achievement_AchievementID",
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

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BetaInvolver = table.Column<bool>(type: "bit", nullable: false),
                    Professional = table.Column<bool>(type: "bit", nullable: false),
                    TimeBetaInvolver = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeProfessional = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementsID);
                    table.ForeignKey(
                        name: "FK_Achievements_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                unique: true);
        }
    }
}
