using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class AdjustInvolvingTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvolverID",
                table: "Involving",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Involving_InvolverID",
                table: "Involving",
                column: "InvolverID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving",
                column: "InvolverID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving");

            migrationBuilder.DropIndex(
                name: "IX_Involving_InvolverID",
                table: "Involving");

            migrationBuilder.DropColumn(
                name: "InvolverID",
                table: "Involving");
        }
    }
}
