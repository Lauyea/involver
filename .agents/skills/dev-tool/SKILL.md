---
name: dev-tool
description: 在 Involver 的 Tools/ 新增或修改開發用工具（模擬器、資料產生器、檢查腳本、報表）。工具一律用 .NET 10 單檔程式（dotnet run xxx.cs），不加進方案，不部署。涵蓋檔頭格式、只能連本機資料庫、識別測試資料與清除模式、README 登記。Use when writing a developer-only script, seeder, simulator or checker under Tools/.
---

# 開發用工具（Tools/）

工具不屬於任何專案、不會部署。每個工具放在自己的資料夾，用 .NET 10 的單檔程式執行。現有工具列在 `Tools/README.md`，寫新工具前先看有沒有可以擴充的。

## 結構

```
Tools/<PascalCase 名稱>/<小寫短名>.cs
```

檔頭固定格式：

```csharp
#:project ../../DataAccess/DataAccess.csproj    // 需要 ApplicationDbContext 或模型時才加
#:package Some.Package@1.2.3                   // 盡量不用；需要時固定版本
#:property TargetFramework=net10.0
#:property PublishAot=false

// <一句話說明工具做什麼、為什麼需要>
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 以上）：
//   dotnet run xxx.cs -- <指令>    <說明>
//
// <連線字串、環境變數、注意事項>
```

- 用 top-level statements，加上 `Console.OutputEncoding = Encoding.UTF8;`。輸出訊息用繁體中文。
- 結束代碼：0 成功；1 失敗或有發現問題；2 用法錯誤。
- 需要儲存庫路徑時，從目前目錄往上找 `Involver.sln`，這樣在工具資料夾或根目錄都能執行。
- **要在工具自己的資料夾執行。** 在有 `.csproj` 的資料夾（例如 `Involver/`）執行 `dotnet run <路徑>/xxx.cs`，會改成執行那個專案。
- 參考範例：`Tools/RecommendationSimulator/recsim.cs`（寫入資料庫）、`Tools/DevSeed/devseed.cs`（測試帳號）、`Tools/FrontendLint/felint.cs`（純靜態檢查，有 `--self-test`）。

## 會寫入資料的工具

- **只能連本機資料庫**：解析連線字串的 DataSource，不是 `.`、`(local`、`localhost`、`127.0.0.1` 或本機名稱就直接停止（照抄 `recsim.cs` 開頭的檢查）。
- 連線字串用環境變數覆寫，預設 `Server=.\SQLEXPRESS;Database=Involver;Trusted_Connection=True;TrustServerCertificate=True`。
- **測試資料要能辨識**：ProfileID 用固定前綴（例如 `00000000-0238-`、`00000000-de00-`），標題用固定前綴（例如 `[DEV]`）。新工具要選一個沒用過的前綴，並寫進檔頭。
- **要有清除模式**（`clean`），而且建立前先清一次，讓工具可以重複執行。清除參照會員的資料時，可以沿用依外鍵逐表刪除的寫法（見 `devseed.cs` 的 `CleanAsync`）。
- 大量寫入前建議先備份：`Tools/DbSnapshot`。

## 完成時

1. 實際執行每個指令，貼出輸出確認結果正確；有清除模式的話，清除後再建立一次。
2. 在 `Tools/README.md` 的表格加一行（資料夾、用途、主要用法）。
3. 工具與某個 skill 的流程有關時，在那個 skill 的 SKILL.md 加上使用時機，並跑 `Tools/AgentSkills` 的 `check`。
