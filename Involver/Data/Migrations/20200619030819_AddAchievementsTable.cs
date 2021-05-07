using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class AddAchievementsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DailyLogin",
                table: "Missions",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementsID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BetaInvolver = table.Column<bool>(nullable: false),
                    Professional = table.Column<bool>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementsID);
                    table.ForeignKey(
                        name: "FK_Achievements_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                unique: true,
                filter: "[ProfileID] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropColumn(
                name: "DailyLogin",
                table: "Missions");
        }
    }
}
