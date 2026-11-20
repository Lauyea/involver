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
   - 加上 NOT NULL 欄位時，要給既有資料預設值，或先用 SQL 補資料。
   - 刪除欄位或資料表前，確認沒有排程（`InvolverRoutineWork`）或工具（`Tools/`）還在用。
5. **手寫 SQL 一律包 EXEC**（見下一節）。
6. **套用到本機**：
   ```
   dotnet ef database update --project DataAccess --startup-project Involver
   ```
7. **部署前檢查**（必做）：
   ```
   cd Tools/MigrationCheck
   dotnet run migcheck.cs                 # 靜態檢查加兩個情境
   dotnet run migcheck.cs -- --with-data  # 加跑一份含本機資料的複本，資料搬移類的 migration 一定要跑
   ```
8. **跑測試**：測試用 SQLite（`InvolverTest/Infrastructure`）。SQL Server 專屬語法（例如 filtered index、`HasDefaultValueSql`）可能需要在測試的 DbContext 設定裡另外處理。
   ```
   dotnet test
   ```
9. 在 PR 的「測試」段落寫上 migcheck 的結果，並提醒部署時要勾 `run_migration`。

## 手寫 SQL 一律包 EXEC（重要）

EF Core 10 的 idempotent script 把一個 migration 放在**同一個批次**，不會用 EXEC 包住 `migrationBuilder.Sql`。SQL Server 會先編譯整個批次才執行，所以手寫 SQL 只要參照到同一個 migration 才新增、改名或刪除的欄位，就會出現 `Invalid column name`。

而且 `IF NOT EXISTS (… __EFMigrationsHistory …)` 的略過判斷發生在編譯**之後**，所以即使這個 migration 已經套用過，**之後每一次部署都會失敗**。

```csharp
// 錯：同一個 migration 新增的欄位，idempotent script 編譯時會找不到
migrationBuilder.Sql("UPDATE [Orders] SET [NewColumn] = [OldColumn] WHERE [NewColumn] IS NULL");

// 對：包 EXEC(N'...')，單引號要變成兩個
migrationBuilder.Sql($"EXEC(N'{sql.Replace("'", "''")}');");
```

`20261118012621_UnifyTags.cs` 裡有現成的 `Exec(...)` 輔助方法，可以照抄。前置檢查（precondition check）的 SQL 也一樣要包。

已知例外：舊的 `20250922090820_ArticleIntegrationDataTable` 沒有包 EXEC，所以「從空資料庫跑 idempotent script」本來就會失敗。這只影響建立全新環境，不影響正式資料庫。不要為了這個去修改已經部署過的 migration。

## 不要做的事

- 不要修改已經合併到 master 的 migration（正式資料庫已經套用過）。要修正就再加一個新的 migration。
- 不要在 migration 裡依賴 `Involver` 專案的程式碼（例如角色常數）。需要的話直接寫字串。
- 不要對正式資料庫執行 `dotnet ef database update`。正式環境只透過 deploy.yml 部署。

## 出問題時

- **migcheck 的情境失敗**：加 `--keep` 保留 `InvolverMigCheck_*` 資料庫，用 SSMS 檢查。修好後再跑一次。
- **本機資料庫被弄壞**：`cd Tools/DbSnapshot && dotnet run dbsnap.cs -- restore <名稱>`（會中斷所有連線，請先停掉 dev server）。
