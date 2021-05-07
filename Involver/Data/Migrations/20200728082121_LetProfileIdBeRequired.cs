using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class LetProfileIdBeRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_Profile_ProfileID",
                table: "Achievements");

            migrationBuilder.DropForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree");

            migrationBuilder.DropForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel");

            migrationBuilder.DropIndex(
                name: "IX_Missions_ProfileID",
                table: "Missions");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements");

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Novel",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Missions",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvolverID",
                table: "Involving",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FollowerID",
                table: "Follow",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Comment",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Article",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Agree",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Achievements",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ProfileID",
                table: "Missions",
                column: "ProfileID",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow",
                column: "FollowerID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving",
                column: "InvolverID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Achievements_Profile_ProfileID",
                table: "Achievements");

            migrationBuilder.DropForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree");

            migrationBuilder.DropForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel");

            migrationBuilder.DropIndex(
                name: "IX_Missions_ProfileID",
                table: "Missions");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements");

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Novel",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Missions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "InvolverID",
                table: "Involving",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "FollowerID",
                table: "Follow",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Comment",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Article",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Agree",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProfileID",
                table: "Achievements",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ProfileID",
                table: "Missions",
                column: "ProfileID",
                unique: true,
                filter: "[ProfileID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                unique: true,
                filter: "[ProfileID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Achievements_Profile_ProfileID",
                table: "Achievements",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow",
                column: "FollowerID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving",
                column: "InvolverID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
