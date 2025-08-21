using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameArticleToArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleArticleTag_Article_ArticlesArticleID",
                table: "ArticleArticleTag");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewer_Article_ArticleID",
                table: "ArticleViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewIp_Article_ArticlesArticleID",
                table: "ArticleViewIp");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Article_ArticleID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Article_ArticleID",
                table: "Involving");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Article",
                table: "Article");

            migrationBuilder.RenameTable(
                name: "Article",
                newName: "Articles");

            migrationBuilder.RenameIndex(
                name: "IX_Article_ProfileID",
                table: "Articles",
                newName: "IX_Articles_ProfileID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Articles",
                table: "Articles",
                column: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleArticleTag_Articles_ArticlesArticleID",
                table: "ArticleArticleTag",
                column: "ArticlesArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Profile_ProfileID",
                table: "Articles",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewer_Articles_ArticleID",
                table: "ArticleViewer",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewIp_Articles_ArticlesArticleID",
                table: "ArticleViewIp",
                column: "ArticlesArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Articles_ArticleID",
                table: "Comment",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Articles_ArticleID",
                table: "Involving",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleArticleTag_Articles_ArticlesArticleID",
                table: "ArticleArticleTag");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Profile_ProfileID",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewer_Articles_ArticleID",
                table: "ArticleViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewIp_Articles_ArticlesArticleID",
                table: "ArticleViewIp");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Articles_ArticleID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Articles_ArticleID",
                table: "Involving");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Articles",
                table: "Articles");

            migrationBuilder.RenameTable(
                name: "Articles",
                newName: "Article");

            migrationBuilder.RenameIndex(
                name: "IX_Articles_ProfileID",
                table: "Article",
                newName: "IX_Article_ProfileID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Article",
                table: "Article",
                column: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Article_Profile_ProfileID",
                table: "Article",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleArticleTag_Article_ArticlesArticleID",
                table: "ArticleArticleTag",
                column: "ArticlesArticleID",
                principalTable: "Article",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewer_Article_ArticleID",
                table: "ArticleViewer",
                column: "ArticleID",
                principalTable: "Article",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewIp_Article_ArticlesArticleID",
                table: "ArticleViewIp",
                column: "ArticlesArticleID",
                principalTable: "Article",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Article_ArticleID",
                table: "Comment",
                column: "ArticleID",
                principalTable: "Article",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Article_ArticleID",
                table: "Involving",
                column: "ArticleID",
                principalTable: "Article",
                principalColumn: "ArticleID");
        }
    }
}
