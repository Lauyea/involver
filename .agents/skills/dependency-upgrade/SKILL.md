---
name: dependency-upgrade
description: Involver 的套件升級與弱點修正：升級 NuGet 套件、修 NuGet 弱點警告（含轉移相依）、用 LibMan 升級前端函式庫（Bootstrap、Vue、CKEditor 5 等）並同步 import map 時使用。Use when updating NuGet or LibMan packages, fixing vulnerability warnings (NU1901-NU1904), or adding a front-end library.
---

# 套件升級

原則：沒有必要不新增相依套件（AGENTS.md「相容性與相依性」）。要新增時，先在 PR 說明原因。

## NuGet

專案：`Involver`、`DataAccess`、`InvolverRoutineWork`、`InvolverTest`。各專案的 csproj 各自管理版本，沒有使用 Central Package Management。

1. **盤點**（在儲存庫根目錄）：
   ```
   dotnet list Involver.sln package --outdated
   dotnet list Involver.sln package --vulnerable --include-transitive
   ```
2. **追蹤轉移相依的弱點來源**：
   ```
   dotnet nuget why Involver.sln <有弱點的套件>
   ```
   先找出是哪個直接參考帶進來的，再決定怎麼修：
   - **最好**：升級或移除帶入它的直接參考。例如 #244：DataAccess 參考的 `Microsoft.AspNetCore.Identity` 2.x 在 .NET 10 已由共用框架提供，直接移除就清掉整串舊套件。
   - **其次**：直接參考修正後的版本來覆蓋轉移相依（要在 csproj 寫註解說明原因，日後可以移除）。
3. **升級**：
   ```
   dotnet package update --project <csproj> <套件>[@版本]
   dotnet package update --project Involver.sln --vulnerable   # 只升級有已知弱點的套件
   ```
   或直接改 csproj 的版本。同一家族的套件（`Microsoft.EntityFrameworkCore.*`、`Microsoft.AspNetCore.*`、`Microsoft.Extensions.*`）版本要一致；`dotnet-tools.json` 的 `dotnet-ef` 也要跟 EF Core 版本一致。
4. **確認**：
   ```
   dotnet build
   dotnet test
   ```
   主版號升級時，先查官方的 breaking changes（可用 Microsoft Learn MCP 查 .NET 10 的文件）。

## LibMan（前端函式庫）

設定檔是 `Involver/libman.json`，檔案下載到 `Involver/wwwroot/lib/`，**有進版控**。需要 LibMan CLI：`dotnet tool install -g Microsoft.Web.LibraryManager.Cli`。

1. **改版本**：編輯 `libman.json` 的 `library`（例如 `vue@3.5.22`），或執行：
   ```
   cd Involver
   libman update <函式庫> --to <版本>
   ```
2. **還原檔案**：在 `Involver` 資料夾執行 `libman restore`。用 `git status` 確認 `wwwroot/lib/` 的變更合理，不要有多餘或缺少的檔案。
3. **ES module 函式庫**：檔名或路徑變了，要同步修改 `Involver/Helpers/ImportMapHelper.cs` 的對應表。Vue 必須用含模板編譯器的 `vue.esm-browser.prod.min.js`，因為元件模板直接寫在 .cshtml 裡。
4. **CKEditor 5**：翻譯檔與深色模式樣式都在 `wwwroot/lib/ckeditor5`，升級後要確認編輯器在深色模式的顯示（`site.css` 有 `:root[data-bs-theme]` 的覆寫）。
5. **確認**：
   ```
   cd Tools/FrontendLint
   dotnet run felint.cs   # 會檢查 import map 的每個檔案都存在
   ```
   在瀏覽器打開用到這個函式庫的頁面，確認 console 沒有錯誤。投票與留言用 Vue；新增或編輯文章、小說、章節用 CKEditor；統計頁用圖表。
6. **Bootstrap** 的版本要和 AGENTS.md「相容性與相依性」一致，升級時一併修改 AGENTS.md。

## PR

依 `issue-to-pr`。測試段落列出升級前後的版本，以及 `--vulnerable` 的結果。
