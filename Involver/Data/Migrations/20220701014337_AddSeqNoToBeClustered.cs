using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AddSeqNoToBeClustered : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            #region Drop FK
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

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving");
            #endregion
            
            migrationBuilder.DropPrimaryKey(
                name: "PK_Profile",
                table: "Profile");

            migrationBuilder.AddColumn<int>(
                name: "SeqNo",
                table: "Profile",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profile",
                table: "Profile",
                column: "ProfileID")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Profile_SeqNo",
                table: "Profile",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            #region Add FK
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

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);
            #endregion
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            #region Drop FK
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

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving");
            #endregion

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profile",
                table: "Profile");

            migrationBuilder.DropIndex(
                name: "IX_Profile_SeqNo",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "SeqNo",
                table: "Profile");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profile",
                table: "Profile",
                column: "ProfileID");

            #region Add FK
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

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);
            #endregion
        }
    }
}
