using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class ExternalLoginCanChangeUserNameOneTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanChangeUserName",
                table: "Profile",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanChangeUserName",
                table: "Profile");
        }
    }
}
