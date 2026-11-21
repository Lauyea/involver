---
name: ef-migration
description: Involver 的 EF Core migration 流程：新增或修改 Entity、DbContext 設定、索引、約束、資料搬移（migrationBuilder.Sql）時使用。涵蓋 dotnet ef migrations add、EXEC 包裝規則、部署前用 Tools/MigrationCheck 模擬正式環境的 idempotent script、本機資料庫快照與 deploy.yml 的 run_migration。Use when adding/changing entities, schema, indexes, constraints, seed or data-migration SQL, or before deploying a migration.
---

# EF Core Migration

Involver 採 Code First。DbContext 是 `DataAccess/Data/ApplicationDbContext.cs`，migration 在 `DataAccess/Migrations/`，啟動專案是 `Involver`。

正式環境的部署（`.github/workflows/deploy.yml`，手動觸發並勾選 `run_migration`）會產生**完整的 idempotent script**，用 `sqlcmd -b` 一次跑完。所有規則都是為了讓這份 script 在正式資料庫上能重複執行。

## 步驟

1. **先留快照**（會動到本機資料庫時）：
   ```
   cd Tools/DbSnapshot
   dotnet run dbsnap.cs -- backup before-<功能名稱>
   ```
2. **修改模型**：Entity 放在 `DataAccess/Models/`，Fluent 設定寫在 `ApplicationDbContext.OnModelCreating`。新屬性要有 XML 註解。
3. **產生 migration**（在儲存庫根目錄）：
   ```
   dotnet tool restore
   dotnet ef migrations add <PascalCase名稱> --project DataAccess --startup-project Involver
   ```
   產生錯了就用 `dotnet ef migrations remove --project DataAccess --startup-project Involver` 移除，不要手動刪檔。
4. **檢查產生的檔案**：
   - 改名欄位時，EF 可能產生 Drop + Add（會遺失資料）。這種情況要改成 `RenameColumn`。
   - 加上 NOT NULL 欄位時，要給既有資料預設值；既有資料需要算出來的值時，在同一個 `Up()` 的 `AddColumn` 之後用 SQL 回填（範本見下一節）。
   - 刪除欄位或資料表前，確認沒有排程（`InvolverRoutineWork`）或工具（`Tools/`）還在用。
5. **手寫 SQL 一律包 EXEC**（見下一節）。
6. **套用到本機**：
   ```
   dotnet ef database update --project DataAccess --startup-project Involver
   ```
   連線字串來自 `Involver/appsettings.Development.json`。這個檔案沒有進版控，每台電腦各自一份。出現「此憑證鏈結是由不受信任的授權單位發出的」時，不是資料庫沒啟動，而是那份連線字串少了 `TrustServerCertificate=True`：在本機的檔案補上，或這一次先加 `--connection "<完整連線字串>"`。
7. **有資料搬移或回填時，確認結果是對的**。migration 套用成功只代表 SQL 沒有語法錯誤，不代表算出來的值正確。
   - 用查詢比對回填的值和來源資料，確認沒有任何一筆不一致。例如回填章節數時，比對欄位值和實際 `COUNT(*)`。
   - 本機資料常常涵蓋不到條件裡的每一種情況（例如沒有任何一筆 `IsDeleted = 1`）。這時在交易裡造出那種資料、執行回填的 SQL、檢查結果，最後 `ROLLBACK`，不要留下假資料。
   - 在回報裡寫出比對的筆數，以及哪些情況是用造出來的資料驗證的。
8. **部署前檢查**（必做）：
   ```
   cd Tools/MigrationCheck
   dotnet run migcheck.cs                 # 靜態檢查加兩個情境
   dotnet run migcheck.cs -- --with-data  # 加跑一份含本機資料的複本，資料搬移類的 migration 一定要跑
   ```
9. **跑測試**：測試用 SQLite（`InvolverTest/Infrastructure`）。SQL Server 專屬語法（例如 filtered index、`HasDefaultValueSql`）可能需要在測試的 DbContext 設定裡另外處理。
   ```
   dotnet test
   ```
10. 在 PR 的「測試」段落寫上 migcheck 與資料比對的結果，並提醒部署時要勾 `run_migration`。

## 手寫 SQL 一律包 EXEC（重要）

EF Core 10 的 idempotent script 把一個 migration 放在**同一個批次**，不會用 EXEC 包住 `migrationBuilder.Sql`。SQL Server 會先編譯整個批次才執行，所以手寫 SQL 只要參照到同一個 migration 才新增、改名或刪除的欄位，就會出現 `Invalid column name`。

而且 `IF NOT EXISTS (… __EFMigrationsHistory …)` 的略過判斷發生在編譯**之後**，所以即使這個 migration 已經套用過，**之後每一次部署都會失敗**。

```csharp
// 錯：同一個 migration 新增的欄位，idempotent script 編譯時會找不到
migrationBuilder.Sql("UPDATE [Orders] SET [NewColumn] = [OldColumn] WHERE [NewColumn] IS NULL");

// 對：包 EXEC(N'...')，單引號要變成兩個
migrationBuilder.Sql($"EXEC(N'{sql.Replace("'", "''")}');");
```

前置檢查（precondition check）的 SQL 也一樣要包。

### 範本：新增欄位並回填

`Exec` 輔助方法是每個 migration 自己的 private 方法，需要時複製一份到 migration 類別裡（`20261118012621_UnifyTags.cs` 也是這樣做）。migration 不共用程式碼，是因為已經部署過的 migration 不能再改，共用的方法日後一改就會影響舊的 migration。

SQL 用多行的 raw string（`"""`）寫，比較好讀，也不用處理雙引號。

```csharp
/// <summary>
/// 手寫 SQL 包成 EXEC：idempotent script 把整個 migration 放在同一個批次，
/// 直接參照這個 migration 才新增的欄位，編譯時會出現 Invalid column name。
/// </summary>
private static string Exec(string sql) => $"EXEC(N'{sql.Replace("'", "''")}');";

protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "EpisodeCount",
        table: "Novels",
        type: "int",
        nullable: false,
        defaultValue: 0);

    // 回填：沒有對應資料的列維持預設值
    migrationBuilder.Sql(Exec("""
        UPDATE n
        SET n.[EpisodeCount] = e.[Cnt]
        FROM [Novels] n
        INNER JOIN (
            SELECT [NovelID], COUNT(*) AS [Cnt]
            FROM [Episodes]
            WHERE [IsDeleted] = 0
            GROUP BY [NovelID]
        ) e ON e.[NovelID] = n.[NovelID]
        """));
}
```

`Down()` 只要移除欄位，不需要還原回填的資料。

已知例外：舊的 `20250922090820_ArticleIntegrationDataTable` 沒有包 EXEC，所以「從空資料庫跑 idempotent script」本來就會失敗。這只影響建立全新環境，不影響正式資料庫。不要為了這個去修改已經部署過的 migration。

## 不要做的事

- 不要修改已經合併到 master 的 migration（正式資料庫已經套用過）。要修正就再加一個新的 migration。
- 不要在 migration 裡依賴 `Involver` 專案的程式碼（例如角色常數）。需要的話直接寫字串。
- 不要對正式資料庫執行 `dotnet ef database update`。正式環境只透過 deploy.yml 部署。

## 出問題時

- **migcheck 的情境失敗**：加 `--keep` 保留 `InvolverMigCheck_*` 資料庫，用 SSMS 檢查。修好後再跑一次。
- **本機資料庫被弄壞**：`cd Tools/DbSnapshot && dotnet run dbsnap.cs -- restore <名稱>`（會中斷所有連線，請先停掉 dev server）。
