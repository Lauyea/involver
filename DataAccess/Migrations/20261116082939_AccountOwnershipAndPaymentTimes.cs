using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AccountOwnershipAndPaymentTimes : Migration
    {
        /// <summary>
        /// 修改前的資料檢查：有任何不符合的資料就 THROW，整個 migration rollback，不修改資料。
        /// 與 DataAccess/Scripts/Issue219-AccountOwnership-PreMigrationCheck.sql 的檢查項目一致。
        /// </summary>
        private const string PreconditionCheckSql = """
            DECLARE @problems nvarchar(max) = N'';

            -- Profile 必須有對應的登入帳號，才能加 1:1 外鍵
            IF EXISTS (SELECT 1 FROM [Profiles] p WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[Id] = p.[ProfileID]))
                SET @problems += N' Profiles 找不到對應的 AspNetUsers;';

            -- 移除 AspNetUsers.Prime/Banned 前，兩邊必須一致，否則以 Profiles 為準會遺失狀態
            IF EXISTS (SELECT 1 FROM [Profiles] p JOIN [AspNetUsers] u ON u.[Id] = p.[ProfileID] WHERE p.[Prime] <> u.[Prime] OR p.[Banned] <> u.[Banned])
                SET @problems += N' Profiles 與 AspNetUsers 的 Prime/Banned 不一致;';
            IF EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE (u.[Prime] = 1 OR u.[Banned] = 1) AND NOT EXISTS (SELECT 1 FROM [Profiles] p WHERE p.[ProfileID] = u.[Id]))
                SET @problems += N' 沒有 Profile 的帳號有 Prime/Banned 狀態;';

            -- 明文敏感資料欄位必須是空的才移除
            IF EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [BankAccount] IS NOT NULL AND LTRIM(RTRIM([BankAccount])) <> N'')
                SET @problems += N' AspNetUsers.BankAccount 有資料;';
            IF EXISTS (SELECT 1 FROM [ProfitSharings] WHERE [CreditCard] IS NOT NULL AND LTRIM(RTRIM([CreditCard])) <> N'')
                SET @problems += N' ProfitSharings.CreditCard 有資料;';

            -- 擁有者欄位必須指向存在的 Profile
            IF EXISTS (SELECT 1 FROM [Episodes] e WHERE e.[OwnerID] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Profiles] p WHERE p.[ProfileID] = e.[OwnerID]))
                SET @problems += N' Episodes.OwnerID 找不到 Profile;';
            IF EXISTS (SELECT 1 FROM [Votings] v WHERE v.[OwnerID] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Profiles] p WHERE p.[ProfileID] = v.[OwnerID]))
                SET @problems += N' Votings.OwnerID 找不到 Profile;';
            IF EXISTS (SELECT 1 FROM [Votes] v WHERE v.[OwnerID] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Profiles] p WHERE p.[ProfileID] = v.[OwnerID]))
                SET @problems += N' Votes.OwnerID 找不到 Profile;';

            IF @problems <> N''
            BEGIN
                DECLARE @message nvarchar(2048) = LEFT(N'Issue #219 帳號與擁有者 migration 前置檢查失敗，未修改任何資料。請先執行 DataAccess/Scripts/Issue219-AccountOwnership-PreMigrationCheck.sql 查看明細並處理：' + @problems, 2048);
                THROW 50220, @message, 1;
            END
            """;

        /// <summary>
        /// 由綠界原始字串（yyyy/MM/dd HH:mm:ss）回填付款時間；無法解析的留 NULL，原始字串仍保留。
        /// 用 EXEC 包起來，因為同一個 batch 編譯時新欄位還不存在。
        /// </summary>
        private const string BackfillPaymentTimesSql = """
            EXEC(N'UPDATE [Payments]
                SET [PaymentTime] = TRY_CONVERT(datetime2, REPLACE([PaymentDate], N''/'', N''-''), 120),
                    [TradeTime] = TRY_CONVERT(datetime2, REPLACE([TradeDate], N''/'', N''-''), 120)');
            """;

        /// <summary>
        /// Down 時把 Prime/Banned 從 Profiles 還原回 AspNetUsers
        /// </summary>
        private const string RestoreUserFlagsSql = """
            EXEC(N'UPDATE u
                SET u.[Prime] = p.[Prime], u.[Banned] = p.[Banned]
                FROM [AspNetUsers] u
                JOIN [Profiles] p ON p.[ProfileID] = u.[Id]');
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先確認現有資料可以安全修改，不符合就中止，不自動修改或刪除資料
            migrationBuilder.Sql(PreconditionCheckSql);

            migrationBuilder.DropColumn(
                name: "CreditCard",
                table: "ProfitSharings");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Banned",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Prime",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentTime",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TradeTime",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(BackfillPaymentTimesSql);

            migrationBuilder.AddForeignKey(
                name: "FK_Episodes_Profiles_OwnerID",
                table: "Episodes",
                column: "OwnerID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_AspNetUsers_ProfileID",
                table: "Profiles",
                column: "ProfileID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Profiles_OwnerID",
                table: "Votes",
                column: "OwnerID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votings_Profiles_OwnerID",
                table: "Votings",
                column: "OwnerID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Episodes_Profiles_OwnerID",
                table: "Episodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_AspNetUsers_ProfileID",
                table: "Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Profiles_OwnerID",
                table: "Votes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votings_Profiles_OwnerID",
                table: "Votings");

            migrationBuilder.DropColumn(
                name: "PaymentTime",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TradeTime",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "CreditCard",
                table: "ProfitSharings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Banned",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Prime",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(RestoreUserFlagsSql);
        }
    }
}
