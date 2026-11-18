using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// Issue #234 階段 1、2：小說和文章共用同一張 Tags 表、統一標籤格式（NormalizedName + Unique 索引），
    /// 並加上標籤狀態（正式／待審）與建立者。資料只搬一次。
    ///
    /// 步驟：建立 Tags → 舊標籤寫入對照表並算出 NormalizedName → 每個 NormalizedName 只留一筆寫入 Tags
    /// → 更新兩張關聯表的 TagId（同一部作品合併後重複的關聯只留一筆）→ 改 PK、FK → 刪除 NovelTags、ArticleTags
    /// → 最後才建 NormalizedName 的 Unique 索引。
    ///
    /// 套用前請先在正式資料的備份上執行 DataAccess/Scripts/Issue234-PreMigrationCheck.sql，確認會被合併的標籤。
    /// </summary>
    /// <remarks>
    /// NormalizedName 的 SQL 計算是 Involver.Helpers.TagNameNormalizer 的近似版本：
    /// SQL Server 沒有 NFKC，這裡只處理全形英數與標點、英文大小寫、「臺／台」、空白與常見標點。
    /// 少數特殊字元可能和程式算的不同，搬移後可用 GET /api/v1/tags/reports/duplicates 的
    /// NormalizedNameMismatch 列出來確認。
    /// </remarks>
    public partial class UnifyTags : Migration
    {
        /// <summary>
        /// 舊標籤與新標籤的對照表，只在這個 migration 裡使用，最後會刪除。
        /// 用實體表而不是暫存表：每段手寫 SQL 都包成獨立的 EXEC（見 <see cref="Exec"/>），暫存表撐不過一段。
        /// </summary>
        private const string MapTable = "__TagMigrationMap";

        /// <summary>
        /// 統一格式的 SQL（作用在對照表的 NormalizedName）。
        /// 與 DataAccess/Scripts/Issue234-PreMigrationCheck.sql 的計算方式一致。
        /// </summary>
        private const string NormalizeSql = """
            -- 1. 去頭尾空白
            UPDATE [__TagMigrationMap] SET [NormalizedName] = LTRIM(RTRIM([Name]));

            -- 2. 全形英數與標點（U+FF01～U+FF5E）轉半形
            DECLARE @code int = 65281;
            WHILE @code <= 65374
            BEGIN
                UPDATE [__TagMigrationMap]
                SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, NCHAR(@code), NCHAR(@code - 65248));
                SET @code += 1;
            END;

            -- 3. 英文轉小寫
            UPDATE [__TagMigrationMap] SET [NormalizedName] = LOWER([NormalizedName]);

            -- 4. 統一「臺／台」
            UPDATE [__TagMigrationMap] SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, N'臺', N'台');

            -- 5. 去掉空白與標點（Unicode 分類 P*；$ + < = > ^ ` | ~ 等符號不算標點，保留）
            DECLARE @remove nvarchar(400) = N'!"#%&''()*,-./:;?@[\]_{}¡§«¶·»¿‐‑‒–—―‖‗‘’‚‛“”„‟†‡•‣․‥…‧‰‱′″‴‵‶‷‸‹›※‼‽‾‿⁀⁁⁂⁃⁅⁆⁇⁈⁉⁊⁋⁌⁍⁎⁏⁐⁑⁓⁔⁕⁖⁗⁘⁙⁚⁛⁜⁝⁞、。〃〈〉《》「」『』【】〔〕〖〗〘〙〚〛〜〝〞〟〰〽゠・｟｠｡｢｣､･'
                + N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) + NCHAR(12288) + NCHAR(8203) + NCHAR(65279);
            DECLARE @i int = 1;
            DECLARE @length int = DATALENGTH(@remove) / 2;
            WHILE @i <= @length
            BEGIN
                UPDATE [__TagMigrationMap]
                SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, SUBSTRING(@remove, @i, 1), N'');
                SET @i += 1;
            END;

            -- 6. 只有空白或標點的舊標籤：用名稱的小寫，避免變成空字串
            UPDATE [__TagMigrationMap] SET [NormalizedName] = LOWER(LTRIM(RTRIM([Name]))) WHERE [NormalizedName] = N'';
            """;

        /// <summary>
        /// 建立對照表、算出 NormalizedName、合併重複後寫入 Tags，並記下每個舊標籤對應的新 TagId
        /// </summary>
        private const string MigrateTagsSql = $"""
            SET NOCOUNT ON;

            CREATE TABLE [{MapTable}] (
                [Source] char(1) NOT NULL,
                [OldTagId] int NOT NULL,
                [Name] nvarchar(15) NOT NULL,
                [NormalizedName] nvarchar(64) NULL,
                [UsageCount] int NOT NULL,
                [NewTagId] int NULL,
                CONSTRAINT [PK_{MapTable}] PRIMARY KEY ([Source], [OldTagId])
            );

            INSERT INTO [{MapTable}] ([Source], [OldTagId], [Name], [UsageCount])
            SELECT 'N', t.[TagId], t.[Name], (SELECT COUNT(*) FROM [NovelTaggings] nt WHERE nt.[NovelTagsTagId] = t.[TagId])
            FROM [NovelTags] t;

            INSERT INTO [{MapTable}] ([Source], [OldTagId], [Name], [UsageCount])
            SELECT 'A', t.[TagId], t.[Name], (SELECT COUNT(*) FROM [ArticleTaggings] at WHERE at.[ArticleTagsTagId] = t.[TagId])
            FROM [ArticleTags] t;

            {NormalizeSql}

            -- 每個 NormalizedName 只留一筆：顯示名稱取使用次數最多的寫法（同次數時小說優先，再取 ID 小的）。
            -- PARTITION BY 與之後的 Unique 索引都用資料庫預設定序比對，合併結果和索引一致。
            INSERT INTO [Tags] ([Name], [NormalizedName], [Status], [CreatorProfileID], [CreateTime])
            SELECT [Name], [NormalizedName], 0, NULL, GETDATE()
            FROM (
                SELECT [Name], [NormalizedName],
                    ROW_NUMBER() OVER (PARTITION BY [NormalizedName] ORDER BY [UsageCount] DESC, [Source] DESC, [OldTagId]) AS [rn]
                FROM [{MapTable}]) AS [x]
            WHERE [rn] = 1;

            UPDATE m SET [NewTagId] = t.[TagId]
            FROM [{MapTable}] m
            INNER JOIN [Tags] t ON t.[NormalizedName] = m.[NormalizedName];

            IF EXISTS (SELECT 1 FROM [{MapTable}] WHERE [NewTagId] IS NULL)
                THROW 50234, N'Issue #234 migration：有舊標籤找不到對應的新標籤，已中止，未修改任何資料。', 1;
            """;

        /// <summary>
        /// 把 SQL 包成 EXEC，延後到執行時才編譯。
        /// EF 產生的 idempotent script 會把整個 migration 放在同一個 batch（中間沒有 GO），
        /// SQL Server 會在執行前先編譯整個 batch；直接寫 DML 會參照到還沒改名的欄位而失敗。
        /// </summary>
        /// <param name="sql">要執行的 SQL</param>
        /// <returns>EXEC(N'...') 敘述</returns>
        private static string Exec(string sql) => $"EXEC(N'{sql.Replace("'", "''")}');";

        /// <summary>
        /// 把關聯表的 TagId 換成新 ID，並刪除合併後同一部作品重複的關聯
        /// </summary>
        private static string RemapTaggingsSql(string table, string workColumn, string source) => $"""
            UPDATE j SET [TagId] = m.[NewTagId]
            FROM [{table}] j
            INNER JOIN [{MapTable}] m ON m.[Source] = '{source}' AND m.[OldTagId] = j.[TagId];

            WITH [d] AS (
                SELECT ROW_NUMBER() OVER (PARTITION BY [{workColumn}], [TagId] ORDER BY (SELECT 0)) AS [rn]
                FROM [{table}])
            DELETE FROM [d] WHERE [rn] > 1;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 建立 Tags（Unique 索引等合併完重複才建）
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatorProfileID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                    table.ForeignKey(
                        name: "FK_Tags_Profiles_CreatorProfileID",
                        column: x => x.CreatorProfileID,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID");
                });

            // 2. 寫入不重複的 NormalizedName，建立舊 ID → 新 ID 對照
            migrationBuilder.Sql(Exec(MigrateTagsSql));

            // 3. 關聯表：拿掉舊的 PK、FK 與索引，欄位改名
            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_NovelTags_NovelTagsTagId",
                table: "NovelTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_Novels_NovelsNovelID",
                table: "NovelTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings");

            migrationBuilder.DropIndex(
                name: "IX_NovelTaggings_NovelsNovelID",
                table: "NovelTaggings");

            migrationBuilder.RenameColumn(
                name: "NovelTagsTagId",
                table: "NovelTaggings",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "NovelsNovelID",
                table: "NovelTaggings",
                newName: "NovelID");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_ArticleTags_ArticleTagsTagId",
                table: "ArticleTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_Articles_ArticlesArticleID",
                table: "ArticleTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings");

            migrationBuilder.DropIndex(
                name: "IX_ArticleTaggings_ArticlesArticleID",
                table: "ArticleTaggings");

            migrationBuilder.RenameColumn(
                name: "ArticleTagsTagId",
                table: "ArticleTaggings",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "ArticlesArticleID",
                table: "ArticleTaggings",
                newName: "ArticleID");

            // 4. TagId 換成新 ID
            migrationBuilder.Sql(Exec(RemapTaggingsSql("NovelTaggings", "NovelID", "N")));
            migrationBuilder.Sql(Exec(RemapTaggingsSql("ArticleTaggings", "ArticleID", "A")));

            // 5. 新的 PK、索引與 FK（兩張關聯表都有真正的 FK）
            migrationBuilder.AddPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings",
                columns: new[] { "NovelID", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_NovelTaggings_TagId",
                table: "NovelTaggings",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_NovelTaggings_Novels_NovelID",
                table: "NovelTaggings",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelTaggings_Tags_TagId",
                table: "NovelTaggings",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings",
                columns: new[] { "ArticleID", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTaggings_TagId",
                table: "ArticleTaggings",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTaggings_Articles_ArticleID",
                table: "ArticleTaggings",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleTaggings_Tags_TagId",
                table: "ArticleTaggings",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            // 6. 刪除舊表與對照表
            migrationBuilder.DropTable(
                name: "NovelTags");

            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: MapTable);

            // 7. 重複已合併，最後才建 Unique 索引
            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Status",
                table: "Tags",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CreatorProfileID",
                table: "Tags",
                column: "CreatorProfileID");
        }

        /// <summary>
        /// 退回兩張標籤表。標籤 ID 沿用 Tags 的 ID；沒有任何作品使用的標籤、標籤狀態與建立者會遺失，
        /// 合併過的重複標籤也不會拆回原本的寫法。
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.Sql(Exec("""
                SET IDENTITY_INSERT [NovelTags] ON;
                INSERT INTO [NovelTags] ([TagId], [Name])
                SELECT t.[TagId], t.[Name] FROM [Tags] t
                WHERE EXISTS (SELECT 1 FROM [NovelTaggings] j WHERE j.[TagId] = t.[TagId]);
                SET IDENTITY_INSERT [NovelTags] OFF;

                SET IDENTITY_INSERT [ArticleTags] ON;
                INSERT INTO [ArticleTags] ([TagId], [Name])
                SELECT t.[TagId], t.[Name] FROM [Tags] t
                WHERE EXISTS (SELECT 1 FROM [ArticleTaggings] j WHERE j.[TagId] = t.[TagId]);
                SET IDENTITY_INSERT [ArticleTags] OFF;
                """));

            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_Novels_NovelID",
                table: "NovelTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelTaggings_Tags_TagId",
                table: "NovelTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings");

            migrationBuilder.DropIndex(
                name: "IX_NovelTaggings_TagId",
                table: "NovelTaggings");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "NovelTaggings",
                newName: "NovelTagsTagId");

            migrationBuilder.RenameColumn(
                name: "NovelID",
                table: "NovelTaggings",
                newName: "NovelsNovelID");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_Articles_ArticleID",
                table: "ArticleTaggings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleTaggings_Tags_TagId",
                table: "ArticleTaggings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings");

            migrationBuilder.DropIndex(
                name: "IX_ArticleTaggings_TagId",
                table: "ArticleTaggings");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "ArticleTaggings",
                newName: "ArticleTagsTagId");

            migrationBuilder.RenameColumn(
                name: "ArticleID",
                table: "ArticleTaggings",
                newName: "ArticlesArticleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NovelTaggings",
                table: "NovelTaggings",
                columns: new[] { "NovelTagsTagId", "NovelsNovelID" });

            migrationBuilder.CreateIndex(
                name: "IX_NovelTaggings_NovelsNovelID",
                table: "NovelTaggings",
                column: "NovelsNovelID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleTaggings",
                table: "ArticleTaggings",
                columns: new[] { "ArticleTagsTagId", "ArticlesArticleID" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTaggings_ArticlesArticleID",
                table: "ArticleTaggings",
                column: "ArticlesArticleID");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_Name",
                table: "ArticleTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelTags_Name",
                table: "NovelTags",
                column: "Name",
                unique: true);

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
    }
}
