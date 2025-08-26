using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RefactorViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleViewIp");

            migrationBuilder.DropTable(
                name: "NovelViewIp");

            migrationBuilder.DropTable(
                name: "ViewIps");

            migrationBuilder.DropColumn(
                name: "ViewRecordJson",
                table: "Novels");

            migrationBuilder.DropColumn(
                name: "ViewRecordJson",
                table: "Articles");

            migrationBuilder.RenameColumn(
                name: "Views",
                table: "Novels",
                newName: "TotalViews");

            migrationBuilder.RenameColumn(
                name: "Views",
                table: "Articles",
                newName: "TotalViews");

            migrationBuilder.CreateTable(
                name: "Views",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    NovelId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Views_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ArticleID");
                    table.ForeignKey(
                        name: "FK_Views_Novels_NovelId",
                        column: x => x.NovelId,
                        principalTable: "Novels",
                        principalColumn: "NovelID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Views_ArticleId",
                table: "Views",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_NovelId",
                table: "Views",
                column: "NovelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Views");

            migrationBuilder.RenameColumn(
                name: "TotalViews",
                table: "Novels",
                newName: "Views");

            migrationBuilder.RenameColumn(
                name: "TotalViews",
                table: "Articles",
                newName: "Views");

            migrationBuilder.AddColumn<string>(
                name: "ViewRecordJson",
                table: "Novels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewRecordJson",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

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
                        name: "FK_ArticleViewIp_Articles_ArticlesArticleID",
                        column: x => x.ArticlesArticleID,
                        principalTable: "Articles",
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
                        name: "FK_NovelViewIp_Novels_NovelsNovelID",
                        column: x => x.NovelsNovelID,
                        principalTable: "Novels",
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
                name: "IX_ArticleViewIp_ViewIpsId",
                table: "ArticleViewIp",
                column: "ViewIpsId");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewIp_ViewIpsId",
                table: "NovelViewIp",
                column: "ViewIpsId");
        }
    }
}
