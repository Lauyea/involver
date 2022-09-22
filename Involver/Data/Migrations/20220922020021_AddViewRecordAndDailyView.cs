using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AddViewRecordAndDailyView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyView",
                table: "Novel",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ViewRecordJson",
                table: "Novel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyView",
                table: "Article",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ViewRecordJson",
                table: "Article",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyView",
                table: "Novel");

            migrationBuilder.DropColumn(
                name: "ViewRecordJson",
                table: "Novel");

            migrationBuilder.DropColumn(
                name: "DailyView",
                table: "Article");

            migrationBuilder.DropColumn(
                name: "ViewRecordJson",
                table: "Article");
        }
    }
}
