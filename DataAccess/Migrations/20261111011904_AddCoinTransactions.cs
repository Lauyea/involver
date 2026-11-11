using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoinMonthlyBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoinType = table.Column<byte>(type: "tinyint", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "money", nullable: false),
                    Credits = table.Column<decimal>(type: "money", nullable: false),
                    Debits = table.Column<decimal>(type: "money", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "money", nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinMonthlyBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinMonthlyBalances_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoinTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoinType = table.Column<byte>(type: "tinyint", nullable: false),
                    TransactionType = table.Column<short>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "money", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "money", nullable: false),
                    CounterpartyProfileId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReferenceType = table.Column<short>(type: "smallint", nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinTransactions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoinMonthlyBalances_Month",
                table: "CoinMonthlyBalances",
                column: "Month");

            migrationBuilder.CreateIndex(
                name: "IX_CoinMonthlyBalances_ProfileId_CoinType_Month",
                table: "CoinMonthlyBalances",
                columns: new[] { "ProfileId", "CoinType", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_CorrelationId",
                table: "CoinTransactions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_CreatedAt",
                table: "CoinTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_IdempotencyKey",
                table: "CoinTransactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoinTransactions_ProfileId_CreatedAt",
                table: "CoinTransactions",
                columns: new[] { "ProfileId", "CreatedAt" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "CoinType", "TransactionType", "Amount", "BalanceAfter" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoinMonthlyBalances");

            migrationBuilder.DropTable(
                name: "CoinTransactions");
        }
    }
}
