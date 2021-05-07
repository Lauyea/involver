using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class ChangeFollowInvolverProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubscribeNovel",
                table: "Follow",
                newName: "NovelMonthlyInvolver");

            migrationBuilder.RenameColumn(
                name: "SubscribeUser",
                table: "Follow",
                newName: "ProfileMonthlyInvolver");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NovelMonthlyInvolver",
                table: "Follow",
                newName: "SubscribeNovel");

            migrationBuilder.RenameColumn(
                name: "ProfileMonthlyInvolver",
                table: "Follow",
                newName: "SubscribeUser");
        }
    }
}
