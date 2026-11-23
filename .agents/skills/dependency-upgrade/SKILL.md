---
name: dependency-upgrade
description: Involver 的套件升級與弱點修正：升級 NuGet 套件（含「全部升到最新」）、修 NuGet 弱點警告（含轉移相依）、dotnet-ef 工具版本、用 LibMan 升級或新增前端函式庫（Vue、Bootstrap、CKEditor 5、Chart.js 等）並同步 import map 時使用。涵蓋 .NET 主版號與 TargetFramework 的界線、主版號升級前查 breaking changes、在瀏覽器驗證用到函式庫的頁面。Use when updating NuGet or LibMan packages, fixing vulnerability warnings (NU1901-NU1904), or adding a front-end library.
---

# 套件升級

原則：沒有必要不新增相依套件（AGENTS.md「相容性與相依性」）。要新增時，先在 PR 說明原因。

升級前先記下現況，之後才能比對：目前版本、`dotnet build` 的警告清單、測試結果。升級後多出的警告才算新問題；原本就有的不要誤判成升級造成的（例如 `Microsoft.Build` 的 NU1701）。

## NuGet

專案：`Involver`、`DataAccess`、`InvolverRoutineWork`、`InvolverTest`。各專案的 csproj 各自管理版本，沒有使用 Central Package Management。

1. **盤點**（在儲存庫根目錄）：
   ```
   dotnet list Involver.sln package --outdated
   dotnet list Involver.sln package --outdated --highest-minor   # 同一主版號內的最新版
   dotnet list Involver.sln package --vulnerable --include-transitive
   ```
2. **.NET 的主版號跟著 TargetFramework 走**。專案目標是 .NET 10（AGENTS.md），所以 `Microsoft.AspNetCore.*`、`Microsoft.EntityFrameworkCore.*`、`Microsoft.Extensions.*`、`System.*` 這些隨 .NET 發行的套件，只升到最新的 **10.0.x**。`--outdated` 列出的 11.0.0 需要 net11.0，「升到最新」不包含換框架。升級到 .NET 11 是另一個決定（要改 TargetFramework、部署環境、CI、AGENTS.md），在回報裡列為待決定事項，不要自己改。
3. **追蹤轉移相依的弱點來源**：
   ```
   dotnet nuget why Involver.sln <有弱點的套件>
   ```
   先找出是哪個直接參考帶進來的，再決定怎麼修：
   - **最好**：升級或移除帶入它的直接參考。例如 #244：DataAccess 參考的 `Microsoft.AspNetCore.Identity` 2.x 在 .NET 10 已由共用框架提供，直接移除就清掉整串舊套件。
   - **其次**：直接參考修正後的版本來覆蓋轉移相依（要在 csproj 寫註解說明原因，日後可以移除）。
4. **其他套件的主版號升級**（例如 `HtmlSanitizer` 10 → 11）：先讀 release notes 或 GitHub releases，列出 breaking changes，再用 grep 找出專案裡用到它的地方，確認受不受影響。結果寫進回報。例如 HtmlSanitizer 只透過 `CustomHtmlSanitizer` 使用，目前沒有它的單元測試：升級時先補一個測試（允許的標籤、YouTube iframe 保留，`<script>`、`onclick` 被移除），或在瀏覽器送一則含 HTML 的留言確認消毒結果。查不到或風險高，就先不升，在回報說明。
5. **升級**：
   ```
   dotnet package update --project <csproj> <套件>[@版本]
   dotnet package update --project Involver.sln --vulnerable   # 只升級有已知弱點的套件
   ```
   或直接改 csproj 的版本。同一家族的套件在**所有專案**的版本要一致。
6. **dotnet-ef 工具**：儲存庫裡可能不只一份工具清單，用 `git ls-files "*dotnet-tools.json"` 全部找出來。`dotnet ef` 會用離目前目錄最近的那一份，所以每一份的 `dotnet-ef` 都要跟 EF Core 版本一致；過時或多餘的清單要在回報提出（例如 #252 刪掉的 Involver 資料夾裡那份，曾經留著 3.1.1）。
7. **確認**：
   ```
   dotnet build
   dotnet test
   ```
   和升級前的警告清單比對。主版號升級時，先查官方的 breaking changes（可用 Microsoft Learn MCP 查 .NET 的文件）。

## LibMan（前端函式庫）

設定檔是 `Involver/libman.json`，檔案下載到 `Involver/wwwroot/lib/`，**有進版控**。需要 LibMan CLI：`dotnet tool install -g Microsoft.Web.LibraryManager.Cli`。

1. **查最新的穩定版**：LibMan 沒有 outdated 指令，用 `npm view <套件> version`（或 `npm view <套件> dist-tags`）。只選 `latest`，不要選 alpha、beta、rc。
2. **讀 changelog**：看目前版本到新版本之間的 changelog（例如 Vue 的 GitHub `CHANGELOG.md`），列出跟專案有關的修正或行為變更。主版號或次版號升級要特別注意 breaking changes。
3. **改版本並還原檔案**：編輯 `libman.json` 的 `library`（例如 `vue@3.5.46`），在 `Involver` 資料夾執行 `libman restore`。
   - `restore` 會重寫**所有**函式庫的檔案。儲存庫是 `core.autocrlf=true`，LibMan 寫出的是 LF，所以其他函式庫也會出現在 `git status`，但 `git diff` 沒有內容變更，只是換行。用 `git checkout -- Involver/wwwroot/lib/<其他函式庫>` 還原，讓變更只剩這次升級的函式庫。
   - 出現 `LIB003`（檔案寫不進去）通常是檔案被占用（網站正在執行或其他程序），停掉網站後再跑一次。
   - 最後用 `git status` 確認變更的檔案就是 `libman.json` 和預期的檔案，沒有多餘或缺少。
4. **ES module 函式庫**：檔名或路徑變了，要同步修改 `Involver/Helpers/ImportMapHelper.cs` 的對應表。Vue 必須用含模板編譯器的 `vue.esm-browser.prod.min.js`，因為元件模板直接寫在 .cshtml 裡。
5. **CKEditor 5**：翻譯檔與深色模式樣式都在 `wwwroot/lib/ckeditor5`，升級後要確認編輯器在深色模式的顯示（`site.css` 有 `:root[data-bs-theme]` 的覆寫）。
6. **確認**：
   ```
   cd Tools/FrontendLint
   dotnet run felint.cs   # 會檢查 import map 的每個檔案都存在
   ```
   然後在瀏覽器實際操作用到這個函式庫的頁面（啟動與登入見 `run-involver` skill），確認 console 沒有錯誤，並用 `import('<套件>')` 之類的方式確認載入的是新版。用 grep 找出所有用到的地方（例如 `grep -rl "from 'vue'" Involver/wwwroot/js`），至少驗證下表中**會呼叫 API 的那一類**，不要只開一個靜態頁面：

   | 函式庫 | 頁面 | 要做的操作 |
   |---|---|---|
   | Vue | 小說、文章或章節的詳細頁的留言區（`comments.js`）、章節頁的投票（`voting.js`）、`/Functions/BracketBuilder`（不用登入，只是純前端） | 登入後展開留言、送出並刪除一則留言 |
   | CKEditor 5 | 新增或編輯文章、小說、章節 | 輸入文字、插入連結，淺色與深色都看 |
   | Chart.js | `/Creator` 統計頁（作者帳號） | 圖表有畫出來，切換深色模式 |
   | Bootstrap | 首頁、導覽列、任一 modal 與 dropdown | 展開收合正常，深色模式正常 |

7. **Bootstrap** 的版本要和 AGENTS.md「相容性與相依性」一致，升級時一併修改 AGENTS.md。

## 回報與 PR

依 `issue-to-pr`。回報與 PR 的測試段落要有：

- 升級前後的版本表；刻意沒升的套件與原因（例如 11.0.0 需要 .NET 11）。
- `--vulnerable` 的結果。
- 主版號升級查到的 breaking changes 與處理方式；changelog 裡值得注意的項目。
- 驗證了哪些頁面與操作，以及沒有驗證的部分。
