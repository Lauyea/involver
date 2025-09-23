using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ArticleIntegrationDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Accept",
                table: "Articles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ArticleType: 1 for Announcement
            migrationBuilder.Sql(@"
                INSERT INTO Articles (ProfileID, Title, Content, CreateTime, UpdateTime, TotalViews, Type, Block, IsDeleted, IsCommentOrderFixed, TotalCoins, MonthlyCoins, DailyView)
                SELECT p.ProfileID, an.Title, an.Content, an.UpdateTime, an.UpdateTime, an.Views, 1, 0, 0, 0, 0, 0, 0
                FROM Announcements an
                LEFT JOIN AspNetUsers u ON an.OwnerID = u.Id
                LEFT JOIN Profiles p ON u.Id = p.ProfileID;
");

            // ArticleType: 2 for Feedback
            migrationBuilder.Sql(@"
                INSERT INTO Articles (ProfileID, Title, Content, CreateTime, UpdateTime, Accept, Type, Block, IsDeleted, IsCommentOrderFixed, TotalCoins, MonthlyCoins, DailyView, TotalViews)
                SELECT p.ProfileID, f.Title, f.Content, f.UpdateTime, f.UpdateTime, f.Accept, 2, f.Block, 0, 0, 0, 0, 0, 0
                FROM Feedbacks f
                LEFT JOIN AspNetUsers u ON f.OwnerID = u.Id
                LEFT JOIN Profiles p ON u.Id = p.ProfileID;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Announcements_AnnouncementID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Feedbacks_FeedbackID",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Comments_AnnouncementID",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_FeedbackID",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AnnouncementID",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "FeedbackID",
                table: "Comments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accept",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Articles");

            migrationBuilder.AddColumn<int>(
                name: "AnnouncementID",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeedbackID",
                table: "Comments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    AnnouncementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.AnnouncementID);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Accept = table.Column<bool>(type: "bit", nullable: false),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.FeedbackID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AnnouncementID",
                table: "Comments",
                column: "AnnouncementID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_FeedbackID",
                table: "Comments",
                column: "FeedbackID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Announcements_AnnouncementID",
                table: "Comments",
                column: "AnnouncementID",
                principalTable: "Announcements",
                principalColumn: "AnnouncementID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Feedbacks_FeedbackID",
                table: "Comments",
                column: "FeedbackID",
                principalTable: "Feedbacks",
                principalColumn: "FeedbackID");
        }
    }
}
