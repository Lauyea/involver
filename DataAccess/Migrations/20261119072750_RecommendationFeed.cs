using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RecommendationFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryIcon",
                table: "Tags",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryOrder",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "CategoryScope",
                table: "Tags",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCategory",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 小說原本固定的 8 種類型轉成同名、只適用小說（CategoryScope = 1）的分類標籤，再把每部小說的類型加成標籤，最後刪掉 Type 欄位。
            // 已有同名標籤（或別名）就沿用並設為分類；分類標籤一律為正式標籤。
            // 整段包在 EXEC 裡：idempotent script 是同一個 batch，直接寫會因為 IsCategory、Type 欄位在編譯時不存在而失敗
            migrationBuilder.Sql(@"EXEC(N'
                DECLARE @now datetime2 = SYSDATETIME();
                DECLARE @categories TABLE (TypeValue int NOT NULL, Name nvarchar(15) NOT NULL, Icon nvarchar(50) NOT NULL, SortOrder int NOT NULL, TagId int NULL);
                INSERT INTO @categories (TypeValue, Name, Icon, SortOrder) VALUES
                    (0, N''奇幻'', N''fa-hat-wizard'', 1),
                    (1, N''歷史'', N''fa-landmark'', 2),
                    (2, N''愛情'', N''fa-heart'', 3),
                    (3, N''真實'', N''fa-person-walking'', 4),
                    (4, N''現代'', N''fa-city'', 5),
                    (5, N''科幻'', N''fa-rocket'', 6),
                    (6, N''驚悚'', N''fa-ghost'', 7),
                    (7, N''推理'', N''fa-magnifying-glass'', 8);

                UPDATE c SET TagId = COALESCE(
                    (SELECT TOP 1 t.TagId FROM Tags t
                        WHERE t.NormalizedName = c.Name OR t.Name = c.Name
                        ORDER BY CASE WHEN t.NormalizedName = c.Name THEN 0 ELSE 1 END, t.TagId),
                    (SELECT TOP 1 a.TagId FROM TagAliases a WHERE a.NormalizedName = c.Name))
                FROM @categories c;

                INSERT INTO Tags (Name, NormalizedName, Status, CreateTime, IsCategory, CategoryScope, CategoryOrder, CategoryIcon)
                SELECT c.Name, c.Name, 0, @now, 1, 1, c.SortOrder, c.Icon FROM @categories c WHERE c.TagId IS NULL;

                UPDATE c SET TagId = t.TagId
                FROM @categories c JOIN Tags t ON t.NormalizedName = c.Name
                WHERE c.TagId IS NULL;

                UPDATE t SET IsCategory = 1, CategoryScope = 1, CategoryOrder = c.SortOrder, CategoryIcon = c.Icon, Status = 0
                FROM Tags t JOIN @categories c ON c.TagId = t.TagId;

                INSERT INTO NovelTaggings (NovelID, TagId)
                SELECT DISTINCT n.NovelID, c.TagId
                FROM Novels n JOIN @categories c ON n.[Type] = c.TypeValue
                WHERE NOT EXISTS (SELECT 1 FROM NovelTaggings x WHERE x.NovelID = n.NovelID AND x.TagId = c.TagId);
            ')");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Novels");

            migrationBuilder.CreateTable(
                name: "EpisodeReads",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EpisodeId = table.Column<int>(type: "int", nullable: false),
                    FirstReadAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReachedEnd = table.Column<bool>(type: "bit", nullable: false),
                    ReachedEndAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeReads_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "EpisodeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeReads_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfileCategoryPreferences",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileCategoryPreferences", x => new { x.ProfileId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ProfileCategoryPreferences_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileCategoryPreferences_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationFeedbacks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContentType = table.Column<byte>(type: "tinyint", nullable: false),
                    ContentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationImpressions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FeedRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<byte>(type: "tinyint", nullable: false),
                    ContentId = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<byte>(type: "tinyint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SettingVersion = table.Column<int>(type: "int", nullable: true),
                    ShownAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationImpressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRecommendations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContentType = table.Column<byte>(type: "tinyint", nullable: false),
                    ContentId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingVersion = table.Column<int>(type: "int", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecommendations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsCategory_CategoryOrder",
                table: "Tags",
                columns: new[] { "IsCategory", "CategoryOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeReads_EpisodeId",
                table: "EpisodeReads",
                column: "EpisodeId")
                .Annotation("SqlServer:Include", new[] { "ReachedEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeReads_ProfileId_EpisodeId",
                table: "EpisodeReads",
                columns: new[] { "ProfileId", "EpisodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCategoryPreferences_TagId",
                table: "ProfileCategoryPreferences",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_ProfileId_ContentType_ContentId",
                table: "RecommendationFeedbacks",
                columns: new[] { "ProfileId", "ContentType", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationImpressions_FeedRequestId_ContentType_ContentId_Section",
                table: "RecommendationImpressions",
                columns: new[] { "FeedRequestId", "ContentType", "ContentId", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationImpressions_ShownAt",
                table: "RecommendationImpressions",
                column: "ShownAt")
                .Annotation("SqlServer:Include", new[] { "Section", "SettingVersion", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationSettings_IsActive",
                table: "RecommendationSettings",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationSettings_Version",
                table: "RecommendationSettings",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRecommendations_ProfileId_Rank",
                table: "UserRecommendations",
                columns: new[] { "ProfileId", "Rank" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Novels",
                type: "int",
                nullable: true);

            // 還原小說類型：取順序最前面、名稱對得上舊類型的分類（分類標籤與作品上的標籤保留）
            migrationBuilder.Sql(@"EXEC(N'
                UPDATE n SET [Type] = x.TypeValue
                FROM Novels n
                CROSS APPLY (
                    SELECT TOP 1 c.TypeValue
                    FROM NovelTaggings nt
                    JOIN Tags t ON t.TagId = nt.TagId
                    JOIN (VALUES (0, N''奇幻''), (1, N''歷史''), (2, N''愛情''), (3, N''真實''), (4, N''現代''), (5, N''科幻''), (6, N''驚悚''), (7, N''推理'')) c(TypeValue, Name) ON t.NormalizedName = c.Name
                    WHERE nt.NovelID = n.NovelID AND t.IsCategory = 1
                    ORDER BY t.CategoryOrder) x;
            ')");

            migrationBuilder.DropTable(
                name: "EpisodeReads");

            migrationBuilder.DropTable(
                name: "ProfileCategoryPreferences");

            migrationBuilder.DropTable(
                name: "RecommendationFeedbacks");

            migrationBuilder.DropTable(
                name: "RecommendationImpressions");

            migrationBuilder.DropTable(
                name: "RecommendationSettings");

            migrationBuilder.DropTable(
                name: "UserRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_Tags_IsCategory_CategoryOrder",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CategoryIcon",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CategoryOrder",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CategoryScope",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsCategory",
                table: "Tags");

        }
    }
}
