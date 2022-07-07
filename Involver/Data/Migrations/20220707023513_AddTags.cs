using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class AddTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "NovelTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelTags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "ArticleArticleTag",
                columns: table => new
                {
                    ArticleTagsTagId = table.Column<int>(type: "int", nullable: false),
                    ArticlesArticleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleArticleTag", x => new { x.ArticleTagsTagId, x.ArticlesArticleID });
                    table.ForeignKey(
                        name: "FK_ArticleArticleTag_Article_ArticlesArticleID",
                        column: x => x.ArticlesArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleArticleTag_ArticleTags_ArticleTagsTagId",
                        column: x => x.ArticleTagsTagId,
                        principalTable: "ArticleTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NovelNovelTag",
                columns: table => new
                {
                    NovelTagsTagId = table.Column<int>(type: "int", nullable: false),
                    NovelsNovelID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelNovelTag", x => new { x.NovelTagsTagId, x.NovelsNovelID });
                    table.ForeignKey(
                        name: "FK_NovelNovelTag_Novel_NovelsNovelID",
                        column: x => x.NovelsNovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelNovelTag_NovelTags_NovelTagsTagId",
                        column: x => x.NovelTagsTagId,
                        principalTable: "NovelTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleArticleTag_ArticlesArticleID",
                table: "ArticleArticleTag",
                column: "ArticlesArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_NovelNovelTag_NovelsNovelID",
                table: "NovelNovelTag",
                column: "NovelsNovelID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleArticleTag");

            migrationBuilder.DropTable(
                name: "NovelNovelTag");

            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: "NovelTags");
        }
    }
}
