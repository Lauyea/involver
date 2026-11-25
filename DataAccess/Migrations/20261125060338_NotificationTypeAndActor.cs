using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTypeAndActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorID",
                table: "Notifications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                table: "Notifications",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 回填舊通知的種類，讓舊通知也有對應的圖示；比對的是舊版 NotificationSetter 組出的標題開頭。
            // 順序有關係：「有人在你的評論：」（留言）要先於「有人在你的%留下評論：」（評論）。
            migrationBuilder.Sql(Exec("""
                UPDATE [Notifications] SET [Type] = 2
                WHERE [Type] = 0 AND [Title] LIKE N'有人在你的評論：「%留言給你。%';

                UPDATE [Notifications] SET [Type] = 3
                WHERE [Type] = 0 AND [Title] LIKE N'有人在你的評論「%留下了一個讚。%';

                UPDATE [Notifications] SET [Type] = 4
                WHERE [Type] = 0 AND [Title] LIKE N'有人在你的留言「%留下了一個讚。%';

                UPDATE [Notifications] SET [Type] = 1
                WHERE [Type] = 0 AND [Title] LIKE N'有人在你的%留下評論：%';

                UPDATE [Notifications] SET [Type] = 5
                WHERE [Type] = 0 AND [Title] LIKE N'你的意見《%被接受了。%';
                """));
        }

        /// <summary>
        /// 手寫 SQL 包成 EXEC：idempotent script 把整個 migration 放在同一個批次，
        /// 直接參照這個 migration 才新增的欄位，編譯時會出現 Invalid column name。
        /// </summary>
        private static string Exec(string sql) => $"EXEC(N'{sql.Replace("'", "''")}');";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorID",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Excerpt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");
        }
    }
}
