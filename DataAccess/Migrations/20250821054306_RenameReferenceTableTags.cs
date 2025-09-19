using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameReferenceTableTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleArticleTag_ArticleTags_ArticleTagsTagId",
                table: "ArticleArticleTag");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleArticleTag_Articles_ArticlesArticleID",
                table: "ArticleArticleTag");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelNovelTag_NovelTags_NovelTagsTagId",
                table: "NovelNovelTag");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelNovelTag_Novels_NovelsNovelID",
                table: "NovelNovelTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NovelNovelTag",
                table: "NovelNovelTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleArticleTag",
                table: "ArticleArticleTag");

            migrationBuilder.RenameTable(
                name: "NovelNovelTag",
                newName: "NovelTaggings");

            migrationBuilder.RenameTable(
                name: "ArticleArticleTag",
                newName: "ArticleTaggings");

            migrationBuilder.RenameIndex(
                name: "IX_NovelNovelTag_NovelsNovelID",
                table: "NovelTaggings",
                newName: "IX_NovelTaggings_NovelsNovelID");

            migrationBuilder.RenameIndex(
                name: "IX_ArticleArticleTag_ArticlesArticleID",
                table: "ArticleTaggings",
                newName: "IX_ArticleTaggings_ArticlesArticleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings",
                columns: new[] { "NovelTagsTagId", "NovelsNovelID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings",
                columns: new[] { "ArticleTagsTagId", "ArticlesArticleID" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTaggings_ArticleTags_ArticleTagsTagId",
                table: "ArticleTaggings",
                column: "ArticleTagsTagId",
                principalTable: "ArticleTags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTaggings_Articles_ArticlesArticleID",
                table: "ArticleTaggings",
                column: "ArticlesArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelTaggings_NovelTags_NovelTagsTagId",
                table: "NovelTaggings",
                column: "NovelTagsTagId",
                principalTable: "NovelTags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelTaggings_Novels_NovelsNovelID",
                table: "NovelTaggings",
                column: "NovelsNovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_ArticleTags_ArticleTagsTagId",
                table: "ArticleTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_Articles_ArticlesArticleID",
                table: "ArticleTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_NovelTags_NovelTagsTagId",
                table: "NovelTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_Novels_NovelsNovelID",
                table: "NovelTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings");

            migrationBuilder.RenameTable(
                name: "NovelTaggings",
                newName: "NovelNovelTag");

            migrationBuilder.RenameTable(
                name: "ArticleTaggings",
                newName: "ArticleArticleTag");

            migrationBuilder.RenameIndex(
                name: "IX_NovelTaggings_NovelsNovelID",
                table: "NovelNovelTag",
                newName: "IX_NovelNovelTag_NovelsNovelID");

            migrationBuilder.RenameIndex(
                name: "IX_ArticleTaggings_ArticlesArticleID",
                table: "ArticleArticleTag",
                newName: "IX_ArticleArticleTag_ArticlesArticleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NovelNovelTag",
                table: "NovelNovelTag",
                columns: new[] { "NovelTagsTagId", "NovelsNovelID" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleArticleTag",
                table: "ArticleArticleTag",
                columns: new[] { "ArticleTagsTagId", "ArticlesArticleID" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleArticleTag_ArticleTags_ArticleTagsTagId",
                table: "ArticleArticleTag",
                column: "ArticleTagsTagId",
                principalTable: "ArticleTags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleArticleTag_Articles_ArticlesArticleID",
                table: "ArticleArticleTag",
                column: "ArticlesArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelNovelTag_NovelTags_NovelTagsTagId",
                table: "NovelNovelTag",
                column: "NovelTagsTagId",
                principalTable: "NovelTags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelNovelTag_Novels_NovelsNovelID",
                table: "NovelNovelTag",
                column: "NovelsNovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}