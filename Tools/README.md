# Tools

開發用的工具，不屬於任何專案、不會部署。每個工具放在自己的資料夾，用 .NET 10 的單檔程式（`dotnet run xxx.cs`）執行，不需要另外建專案。用法都寫在各檔案的開頭；除非另外註明，請在工具自己的資料夾執行。

| 資料夾 | 用途 | 常用指令 |
|---|---|---|
| `AgentSkills` | 建立 `.claude/skills` → `.agents/skills` 的本機 junction，並檢查所有 skill 的格式與內文提到的檔案 | `dotnet run skills.cs` |
| `DevSeed` | 開發用測試帳號（讀者、作者、特約作家、被封鎖、總管理員、管理者、新會員）與少量作品，搭配登入頁的「以使用者 ID 登入」 | `dotnet run devseed.cs -- seed` |
| `DbSnapshot` | 本機 Involver 資料庫的備份與還原（還原成另一個資料庫或覆蓋原本的） | `dotnet run dbsnap.cs -- backup <名稱>` |
| `MigrationCheck` | 部署前模擬 deploy.yml 的 idempotent script：靜態檢查 EXEC 包裝，並在測試資料庫跑「已套用全部」與「停在前一版」兩個情境 | `dotnet run migcheck.cs` |
| `PageInventory` | 列出所有 Razor Pages 的路由、需要的登入、policy 或角色，以及 handler | `dotnet run pages.cs -- --filter Admin` |
| `FormatCheck` | 只檢查改到的 C# 行：空白格式與命名規則（.editorconfig）。CI 也會對 PR 執行 | `dotnet run fmtcheck.cs` |
| `FrontendLint` | 前端規範檢查：寫死的顏色、CSS 屬性排序、`var`、雙引號、`==`、全域 Vue、import map | `dotnet run felint.cs` |
| `AffectedTests` | 從變更找出相關的測試類別，只跑那些測試 | `dotnet run affected.cs -- --run` |
| `UiAudit` | 在瀏覽器執行的 UI 檢查（水平捲動、aria-label、觸控目標、alt、對比），不是 .NET 程式 | 把 `ui-audit.js` 的內容貼到 DevTools Console |
| `RecommendationSimulator` | 推薦演算法模擬器：在本機資料庫灌入模擬的作品與讀者行為，比較不同權重的推薦結果、測試防灌設定 | `dotnet run recsim.cs -- seed` |

注意：

- 會寫入資料的工具只能連本機資料庫。
- 模擬資料都有固定的識別方式（例如 ProfileID 開頭：`00000000-0238-` 是推薦模擬器、`00000000-de00-` 是 DevSeed），工具要提供清除模式。
- 不要在有 `.csproj` 的資料夾（例如 `Involver/`）執行 `dotnet run <路徑>/xxx.cs`，那樣會改成執行該專案。
- 新增工具的寫法見 `.agents/skills/dev-tool/SKILL.md`。
