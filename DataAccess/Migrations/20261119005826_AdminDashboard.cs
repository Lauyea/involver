using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdminDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<short>(type: "smallint", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Before = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    After = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "money", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidByProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatorPayouts_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyContentStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ContentType = table.Column<byte>(type: "tinyint", nullable: false),
                    ContentId = table.Column<int>(type: "int", nullable: false),
                    OwnerProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    UniqueReaders = table.Column<int>(type: "int", nullable: false),
                    NewFollows = table.Column<int>(type: "int", nullable: false),
                    NewComments = table.Column<int>(type: "int", nullable: false),
                    CoinsReceived = table.Column<decimal>(type: "money", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyContentStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailySiteStats",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalUsers = table.Column<int>(type: "int", nullable: false),
                    NewUsers = table.Column<int>(type: "int", nullable: false),
                    DailyActiveUsers = table.Column<int>(type: "int", nullable: false),
                    MonthlyActiveUsers = table.Column<int>(type: "int", nullable: false),
                    PrimeUsers = table.Column<int>(type: "int", nullable: false),
                    ProfessionalUsers = table.Column<int>(type: "int", nullable: false),
                    Novels = table.Column<int>(type: "int", nullable: false),
                    Articles = table.Column<int>(type: "int", nullable: false),
                    Episodes = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<int>(type: "int", nullable: false),
                    NewNovels = table.Column<int>(type: "int", nullable: false),
                    NewArticles = table.Column<int>(type: "int", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    WeeklyVoters = table.Column<int>(type: "int", nullable: false),
                    MonthlyPayingSupporters = table.Column<int>(type: "int", nullable: false),
                    MonthlyActiveCreators = table.Column<int>(type: "int", nullable: false),
                    TopUpAmount = table.Column<decimal>(type: "money", nullable: false),
                    TopUpCount = table.Column<int>(type: "int", nullable: false),
                    RevenueShareAmount = table.Column<decimal>(type: "money", nullable: false),
                    RealCoinsSpent = table.Column<decimal>(type: "money", nullable: false),
                    VirtualCoinsIssued = table.Column<decimal>(type: "money", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySiteStats", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "KpiTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metric = table.Column<short>(type: "smallint", nullable: false),
                    Period = table.Column<byte>(type: "tinyint", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiTargets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<byte>(type: "tinyint", nullable: false),
                    Amount = table.Column<decimal>(type: "money", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagAliases",
                columns: table => new
                {
                    TagAliasId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagAliases", x => x.TagAliasId);
                    table.ForeignKey(
                        name: "FK_TagAliases_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentTime",
                table: "Payments",
                column: "PaymentTime");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Action_CreatedAt",
                table: "AdminAuditLogs",
                columns: new[] { "Action", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActorProfileId_CreatedAt",
                table: "AdminAuditLogs",
                columns: new[] { "ActorProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAt",
                table: "AdminAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorPayouts_PaidAt",
                table: "CreatorPayouts",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorPayouts_ProfileId_Month",
                table: "CreatorPayouts",
                columns: new[] { "ProfileId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyContentStats_ContentType_ContentId_Date",
                table: "DailyContentStats",
                columns: new[] { "ContentType", "ContentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyContentStats_Date",
                table: "DailyContentStats",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyContentStats_OwnerProfileId_Date",
                table: "DailyContentStats",
                columns: new[] { "OwnerProfileId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_KpiTargets_Metric_Period_PeriodStart",
                table: "KpiTargets",
                columns: new[] { "Metric", "Period", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteExpenses_Date",
                table: "SiteExpenses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TagAliases_NormalizedName",
                table: "TagAliases",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagAliases_TagId",
                table: "TagAliases",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "CreatorPayouts");

            migrationBuilder.DropTable(
                name: "DailyContentStats");

            migrationBuilder.DropTable(
                name: "DailySiteStats");

            migrationBuilder.DropTable(
                name: "KpiTargets");

            migrationBuilder.DropTable(
                name: "SiteExpenses");

            migrationBuilder.DropTable(
                name: "TagAliases");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentTime",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Payments");
        }
    }
}
