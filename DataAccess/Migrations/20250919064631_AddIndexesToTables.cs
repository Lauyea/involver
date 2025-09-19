using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Profiles_EnrollmentDate",
                table: "Profiles",
                column: "EnrollmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_ViewDate",
                table: "NovelViewer",
                column: "ViewDate");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_MonthlyCoins",
                table: "Novels",
                column: "MonthlyCoins");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_TotalCoins",
                table: "Novels",
                column: "TotalCoins");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_TotalViews",
                table: "Novels",
                column: "TotalViews");

            migrationBuilder.CreateIndex(
                name: "IX_Novels_UpdateTime",
                table: "Novels",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedDate",
                table: "Notifications",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_UpdateTime",
                table: "Messages",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Involvings_LastTime",
                table: "Involvings",
                column: "LastTime");

            migrationBuilder.CreateIndex(
                name: "IX_Involvings_MonthlyValue",
                table: "Involvings",
                column: "MonthlyValue");

            migrationBuilder.CreateIndex(
                name: "IX_Involvings_TotalValue",
                table: "Involvings",
                column: "TotalValue");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_UpdateTime",
                table: "Follows",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UpdateTime",
                table: "Comments",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_ViewDate",
                table: "ArticleViewer",
                column: "ViewDate");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_UpdateTime",
                table: "Articles",
                column: "UpdateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Agrees_UpdateTime",
                table: "Agrees",
                column: "UpdateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_EnrollmentDate",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_NovelViewer_ViewDate",
                table: "NovelViewer");

            migrationBuilder.DropIndex(
                name: "IX_Novels_MonthlyCoins",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_TotalCoins",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_TotalViews",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Novels_UpdateTime",
                table: "Novels");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedDate",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_UpdateTime",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Involvings_LastTime",
                table: "Involvings");

            migrationBuilder.DropIndex(
                name: "IX_Involvings_MonthlyValue",
                table: "Involvings");

            migrationBuilder.DropIndex(
                name: "IX_Involvings_TotalValue",
                table: "Involvings");

            migrationBuilder.DropIndex(
                name: "IX_Follows_UpdateTime",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UpdateTime",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_ArticleViewer_ViewDate",
                table: "ArticleViewer");

            migrationBuilder.DropIndex(
                name: "IX_Articles_UpdateTime",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Agrees_UpdateTime",
                table: "Agrees");
        }
    }
}
