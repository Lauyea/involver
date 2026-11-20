---
name: run-involver
description: 在本機啟動 Involver 網站、用不同身分登入並在瀏覽器驗證，結束後釋放資源。涵蓋 dev server 啟動方式（Claude 的 preview_start 或 dotnet run）、本機 SQL Server、開發用免密碼登入、測試帳號（Tools/DevSeed）、找頁面網址與權限（Tools/PageInventory），以及停止伺服器與 dotnet build-server shutdown。Use whenever you need to run the app, log in as a role, or check a page in the browser.
---

# 啟動與驗證 Involver

## 前置條件

- 本機 SQL Server Express：`.\SQLEXPRESS`，資料庫 `Involver`。
- schema 要是最新的：`dotnet ef database update --project DataAccess --startup-project Involver`。
- 沒有 user secrets（SendGrid、外部登入等）也可以啟動；缺少的功能會停用。

## 啟動

- **Claude Code（桌面版）**：用 `preview_start`，名稱 `involver`（設定在 `.claude/launch.json`）。
- **其他 Agent 或終端機**（在儲存庫根目錄，背景執行）：
  ```
  dotnet run --project Involver/Involver.csproj --launch-profile Involver
  ```
  - 網址：http://localhost:5000、https://localhost:5001
  - 環境是 `Development`，會啟用開發用登入，以及 Razor 執行階段編譯（改 .cshtml 不用重啟）。
  - 改 C# 程式碼要重新啟動。

## 用不同身分登入

1. **建立測試帳號**（第一次，或想重設資料時）：
   ```
   cd Tools/DevSeed
   dotnet run devseed.cs -- seed
   ```
   `list` 會列出帳號與 ID：

   | 使用者 ID | 身分 |
   |---|---|
   | `00000000-de00-0000-0000-000000000001` | 讀者（1000 虛擬 In 幣、追蹤作者、留過言） |
   | `00000000-de00-0000-0000-000000000002` | 作者（2 部小說、2 篇文章） |
   | `00000000-de00-0000-0000-000000000003` | 特約作家 |
   | `00000000-de00-0000-0000-000000000004` | 被封鎖 |
   | `00000000-de00-0000-0000-000000000005` | 總管理員（所有 *Administrators） |
   | `00000000-de00-0000-0000-000000000006` | 管理者（所有 *Managers） |
   | `00000000-de00-0000-0000-000000000007` | 新會員 |

2. **登入**：開啟 `/Identity/Account/Login`，在「以使用者 ID 登入」欄位（只在 Development 顯示）貼上 ID 送出。要換身分，先登出再登入。
3. **推薦演算法的測試人物**（`00000000-0238-` 開頭）由 `Tools/RecommendationSimulator` 建立，用法寫在 `recsim.cs` 開頭。

## 找頁面與需要的身分

```
cd Tools/PageInventory
dotnet run pages.cs -- --filter <關鍵字>
```

會列出路由、需要的登入、policy 或角色，以及 handler 名稱。注意：

- 沒有標 `[AllowAnonymous]` 的頁面都需要登入。
- `/Admin` 底下依 policy 細分：總管理員帳號可以看到全部，管理者帳號只能看到一部分，適合用來測試權限。

## 驗證畫面

依 `ui-change` 的第 4 步：淺色、深色、375 px，並執行 `Tools/UiAudit/ui-audit.js`。也要看瀏覽器 console，以及伺服器輸出有沒有錯誤。

## 收尾（一定要做）

驗證完、回報之前就要做，不要等使用者提醒：

1. 停掉 dev server。在 Claude Code 用 `preview_stop`；其他情況結束 `dotnet run` 的行程。
2. 關閉瀏覽器分頁。
3. 執行 `dotnet build-server shutdown`。
4. 不認得的 `dotnet`／`VBCSCompiler` 行程可能是使用者的 IDE 在用，不要自己 kill。

在 Claude Code 裡，`.claude/hooks/check-dev-server.mjs` 會在回合結束時檢查 launch.json 啟動的 server 是否還在跑，還在跑的話會提醒一次。
