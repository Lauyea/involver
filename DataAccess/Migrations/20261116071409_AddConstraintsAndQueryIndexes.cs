using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddConstraintsAndQueryIndexes : Migration
    {
        /// <summary>
        /// 加約束前的資料檢查：有任何不符合的資料就 THROW，整個 migration rollback，不修改資料。
        /// 與 DataAccess/Scripts/Issue219-PreMigrationCheck.sql 的檢查項目一致。
        /// </summary>
        private const string PreconditionCheckSql = """
            DECLARE @problems nvarchar(max) = N'';

            IF EXISTS (SELECT 1 FROM [Comments] WHERE (CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [EpisodeID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleID] IS NULL THEN 0 ELSE 1 END) <> 1)
                SET @problems += N' Comments 父表不是剛好一個;';
            IF EXISTS (SELECT 1 FROM [Involvings] WHERE (CASE WHEN [ProfileID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleID] IS NULL THEN 0 ELSE 1 END) <> 1)
                SET @problems += N' Involvings 對象不是剛好一個;';
            IF EXISTS (SELECT 1 FROM [Follows] WHERE (CASE WHEN [ProfileID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END) <> 1)
                SET @problems += N' Follows 對象不是剛好一個;';
            IF EXISTS (SELECT 1 FROM [Agrees] WHERE (CASE WHEN [CommentID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [MessageID] IS NULL THEN 0 ELSE 1 END) <> 1)
                SET @problems += N' Agrees 對象不是剛好一個;';
            IF EXISTS (SELECT 1 FROM [Views] WHERE (CASE WHEN [ArticleId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelId] IS NULL THEN 0 ELSE 1 END) <> 1)
                SET @problems += N' Views 對象不是剛好一個;';

            IF EXISTS (SELECT 1 FROM [Profiles] GROUP BY [UserName] HAVING COUNT(*) > 1)
                SET @problems += N' Profiles.UserName 重複;';
            IF EXISTS (SELECT 1 FROM [Follows] WHERE [ProfileID] IS NOT NULL GROUP BY [ProfileID], [FollowerID] HAVING COUNT(*) > 1)
                SET @problems += N' Follows 重複追蹤作者;';
            IF EXISTS (SELECT 1 FROM [Follows] WHERE [NovelID] IS NOT NULL GROUP BY [NovelID], [FollowerID] HAVING COUNT(*) > 1)
                SET @problems += N' Follows 重複追蹤小說;';
            IF EXISTS (SELECT 1 FROM [Agrees] WHERE [CommentID] IS NOT NULL GROUP BY [CommentID], [ProfileID] HAVING COUNT(*) > 1)
                SET @problems += N' Agrees 重複按讚留言;';
            IF EXISTS (SELECT 1 FROM [Agrees] WHERE [MessageID] IS NOT NULL GROUP BY [MessageID], [ProfileID] HAVING COUNT(*) > 1)
                SET @problems += N' Agrees 重複按讚回覆;';
            IF EXISTS (SELECT 1 FROM [NovelTags] GROUP BY [Name] HAVING COUNT(*) > 1)
                SET @problems += N' NovelTags.Name 重複;';
            IF EXISTS (SELECT 1 FROM [ArticleTags] GROUP BY [Name] HAVING COUNT(*) > 1)
                SET @problems += N' ArticleTags.Name 重複;';

            IF EXISTS (SELECT 1 FROM [Payments] WHERE LEN([MerchantTradeNo]) > 64)
                SET @problems += N' Payments.MerchantTradeNo 超過 64 字;';
            IF EXISTS (SELECT 1 FROM [Payments] WHERE LEN([InvolverID]) > 450)
                SET @problems += N' Payments.InvolverID 超過 450 字;';
            IF EXISTS (SELECT 1 FROM [Episodes] WHERE LEN([OwnerID]) > 450)
                SET @problems += N' Episodes.OwnerID 超過 450 字;';
            IF EXISTS (SELECT 1 FROM [Votings] WHERE LEN([OwnerID]) > 450)
                SET @problems += N' Votings.OwnerID 超過 450 字;';
            IF EXISTS (SELECT 1 FROM [Votes] WHERE LEN([OwnerID]) > 450)
                SET @problems += N' Votes.OwnerID 超過 450 字;';

            IF @problems <> N''
            BEGIN
                DECLARE @message nvarchar(2048) = LEFT(N'Issue #219 migration 前置檢查失敗，未修改任何資料。請先執行 DataAccess/Scripts/Issue219-PreMigrationCheck.sql 查看明細並處理：' + @problems, 2048);
                THROW 50219, @message, 1;
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先確認現有資料符合新的約束，不符合就中止，不自動修改或刪除資料
            migrationBuilder.Sql(PreconditionCheckSql);

            migrationBuilder.DropIndex(
                name: "IX_Views_ArticleId",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_Views_NovelId",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ProfileID",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Follows_NovelID",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Follows_ProfileID",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Agrees_CommentID",
                table: "Agrees");

            migrationBuilder.DropIndex(
                name: "IX_Agrees_MessageID",
                table: "Agrees");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Votings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Votes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MerchantTradeNo",
                table: "Payments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvolverID",
                table: "Payments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Episodes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votings_OwnerID",
                table: "Votings",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_OwnerID",
                table: "Votes",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Views_ArticleId_CreateTime",
                table: "Views",
                columns: new[] { "ArticleId", "CreateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Views_CreateTime",
                table: "Views",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Views_NovelId_CreateTime",
                table: "Views",
                columns: new[] { "NovelId", "CreateTime" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Views_ExactlyOneTarget",
                table: "Views",
                sql: "(CASE WHEN [ArticleId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserName",
                table: "Profiles",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvolverID",
                table: "Payments",
                column: "InvolverID");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantTradeNo",
                table: "Payments",
                column: "MerchantTradeNo");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_ProfileID_ViewDate",
                table: "NovelViewer",
                columns: new[] { "ProfileID", "ViewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NovelTags_Name",
                table: "NovelTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProfileID_CreatedDate",
                table: "Notifications",
                columns: new[] { "ProfileID", "CreatedDate" })
                .Annotation("SqlServer:Include", new[] { "IsRead" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Involvings_ExactlyOneTarget",
                table: "Involvings",
                sql: "(CASE WHEN [ProfileID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleID] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_NovelID_FollowerID",
                table: "Follows",
                columns: new[] { "NovelID", "FollowerID" },
                unique: true,
                filter: "[NovelID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_ProfileID_FollowerID",
                table: "Follows",
                columns: new[] { "ProfileID", "FollowerID" },
                unique: true,
                filter: "[ProfileID] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Follows_ExactlyOneTarget",
                table: "Follows",
                sql: "(CASE WHEN [ProfileID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_OwnerID",
                table: "Episodes",
                column: "OwnerID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_ExactlyOneParent",
                table: "Comments",
                sql: "(CASE WHEN [NovelID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [EpisodeID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleID] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_ProfileID_ViewDate",
                table: "ArticleViewer",
                columns: new[] { "ProfileID", "ViewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_Name",
                table: "ArticleTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Type_UpdateTime",
                table: "Articles",
                columns: new[] { "Type", "UpdateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Agrees_CommentID_ProfileID",
                table: "Agrees",
                columns: new[] { "CommentID", "ProfileID" },
                unique: true,
                filter: "[CommentID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Agrees_MessageID_ProfileID",
                table: "Agrees",
                columns: new[] { "MessageID", "ProfileID" },
                unique: true,
                filter: "[MessageID] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agrees_ExactlyOneTarget",
                table: "Agrees",
                sql: "(CASE WHEN [CommentID] IS NULL THEN 0 ELSE 1 END + CASE WHEN [MessageID] IS NULL THEN 0 ELSE 1 END) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Votings_OwnerID",
                table: "Votings");

            migrationBuilder.DropIndex(
                name: "IX_Votes_OwnerID",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Views_ArticleId_CreateTime",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_Views_CreateTime",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_Views_NovelId_CreateTime",
                table: "Views");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Views_ExactlyOneTarget",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_UserName",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InvolverID",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantTradeNo",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_NovelViewer_ProfileID_ViewDate",
                table: "NovelViewer");

            migrationBuilder.DropIndex(
                name: "IX_NovelTags_Name",
                table: "NovelTags");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ProfileID_CreatedDate",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Involvings_ExactlyOneTarget",
                table: "Involvings");

            migrationBuilder.DropIndex(
                name: "IX_Follows_NovelID_FollowerID",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Follows_ProfileID_FollowerID",
                table: "Follows");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Follows_ExactlyOneTarget",
                table: "Follows");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_OwnerID",
                table: "Episodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_ExactlyOneParent",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_ArticleViewer_ProfileID_ViewDate",
                table: "ArticleViewer");

            migrationBuilder.DropIndex(
                name: "IX_ArticleTags_Name",
                table: "ArticleTags");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Type_UpdateTime",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Agrees_CommentID_ProfileID",
                table: "Agrees");

            migrationBuilder.DropIndex(
                name: "IX_Agrees_MessageID_ProfileID",
                table: "Agrees");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Agrees_ExactlyOneTarget",
                table: "Agrees");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Votings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Votes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MerchantTradeNo",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvolverID",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerID",
                table: "Episodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Views_ArticleId",
                table: "Views",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_NovelId",
                table: "Views",
                column: "NovelId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProfileID",
                table: "Notifications",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_NovelID",
                table: "Follows",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_ProfileID",
                table: "Follows",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Agrees_CommentID",
                table: "Agrees",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Agrees_MessageID",
                table: "Agrees",
                column: "MessageID");
        }
    }
}
