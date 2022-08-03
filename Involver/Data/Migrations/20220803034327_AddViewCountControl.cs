using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AddViewCountControl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement");

            migrationBuilder.AddColumn<int>(
                name: "SeqNo",
                table: "ProfileAchievement",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement",
                columns: new[] { "ProfileID", "AchievementID" })
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateTable(
                name: "ArticleViewer",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArticleID = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ViewDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleViewer", x => new { x.ProfileID, x.ArticleID })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_ArticleViewer_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleViewer_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "NovelViewer",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NovelID = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ViewDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelViewer", x => new { x.ProfileID, x.NovelID })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_NovelViewer_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelViewer_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "ViewIps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewIps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArticleViewIp",
                columns: table => new
                {
                    ArticlesArticleID = table.Column<int>(type: "int", nullable: false),
                    ViewIpsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleViewIp", x => new { x.ArticlesArticleID, x.ViewIpsId });
                    table.ForeignKey(
                        name: "FK_ArticleViewIp_Article_ArticlesArticleID",
                        column: x => x.ArticlesArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleViewIp_ViewIps_ViewIpsId",
                        column: x => x.ViewIpsId,
                        principalTable: "ViewIps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NovelViewIp",
                columns: table => new
                {
                    NovelsNovelID = table.Column<int>(type: "int", nullable: false),
                    ViewIpsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelViewIp", x => new { x.NovelsNovelID, x.ViewIpsId });
                    table.ForeignKey(
                        name: "FK_NovelViewIp_Novel_NovelsNovelID",
                        column: x => x.NovelsNovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelViewIp_ViewIps_ViewIpsId",
                        column: x => x.ViewIpsId,
                        principalTable: "ViewIps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAchievement_SeqNo",
                table: "ProfileAchievement",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_ArticleID",
                table: "ArticleViewer",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_SeqNo",
                table: "ArticleViewer",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewIp_ViewIpsId",
                table: "ArticleViewIp",
                column: "ViewIpsId");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_NovelID",
                table: "NovelViewer",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_SeqNo",
                table: "NovelViewer",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewIp_ViewIpsId",
                table: "NovelViewIp",
                column: "ViewIpsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleViewer");

            migrationBuilder.DropTable(
                name: "ArticleViewIp");

            migrationBuilder.DropTable(
                name: "NovelViewer");

            migrationBuilder.DropTable(
                name: "NovelViewIp");

            migrationBuilder.DropTable(
                name: "ViewIps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement");

            migrationBuilder.DropIndex(
                name: "IX_ProfileAchievement_SeqNo",
                table: "ProfileAchievement");

            migrationBuilder.DropColumn(
                name: "SeqNo",
                table: "ProfileAchievement");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileAchievement",
                table: "ProfileAchievement",
                columns: new[] { "ProfileID", "AchievementID" })
                .Annotation("SqlServer:Clustered", true);
        }
    }
}
