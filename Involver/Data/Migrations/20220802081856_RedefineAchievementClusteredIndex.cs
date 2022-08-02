using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class RedefineAchievementClusteredIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement",
                columns: new[] { "ProfileID", "AchievementID" })
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAchievement_SeqNo",
                table: "ProfileAchievement",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement");

            migrationBuilder.DropIndex(
                name: "IX_ProfileAchievement_SeqNo",
                table: "ProfileAchievement");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement",
                columns: new[] { "ProfileID", "AchievementID" })
                .Annotation("SqlServer:Clustered", true);
        }
    }
}
