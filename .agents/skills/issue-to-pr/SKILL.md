---
name: issue-to-pr
description: Involver 從 GitHub issue 到 Pull Request 的完整流程：開始做一個 issue、建立分支、commit、開 PR、寫 PR 說明時使用。涵蓋分支命名、master 保護、commit 前取得同意、測試與檢查工具、PR 模板（摘要／變更／測試、Closes #N），以及開完 PR 不查 CI。Use when starting work on an issue, committing, pushing, or opening/describing a PR.
---

# Issue → PR

## 1. 開始之前

- 讀 issue 的完整內容與討論：`gh issue view <編號> --comments`。
- 檢查 issue 是否相依其他 issue（例如需要另一個 issue 先合併）。有的話先告訴使用者，不要自己先做一份骨架。
- 範圍不清楚，或 issue 裡有多個階段時，先和使用者確認這次要做哪些。

## 2. 分支

master 有分支保護，**只能透過 PR 修改**。從最新的 master 開分支：

```
git fetch origin
git switch -c <前綴>/<簡短英文描述> origin/master
```

| 前綴 | 用途 |
|---|---|
| `feature/` | 新功能 |
| `fix/` | 修 bug |
| `debug/` | 調查或除錯 |
| `refactor/` | 重構 |

## 3. 實作中

- 依工作內容使用其他 skill：
  - schema 變更：`ef-migration`
  - 畫面：`ui-change`
  - 互動元件：`vue-view-component`
  - 套件：`dependency-upgrade`
  - 開發工具：`dev-tool`
  - 啟動驗證：`run-involver`
- 快速跑受影響的測試：
  ```
  cd Tools/AffectedTests
  dotnet run affected.cs -- --run
  ```
- 金流相關：實體 In 幣（綠界儲值）尚未上線，`ECPay:Enabled` 預設必須維持 `false`。

## 4. Commit 前的檢查

```
dotnet build
dotnet test
cd Tools/FormatCheck && dotnet run fmtcheck.cs     # 改到的 C# 行：空白格式與命名
cd Tools/FrontendLint && dotnet run felint.cs      # 改到的 CSS / JS 行
```

有 migration 時，再跑 `Tools/MigrationCheck`（見 `ef-migration`）。

## 5. Commit

- **commit 前要先取得使用者同意**：告訴使用者分支、要 commit 的檔案與 commit 訊息，等使用者確認。
  - 在 Claude Code 裡，`.claude/hooks/guard-git.mjs` 會在 commit 時跳出確認視窗，這個視窗就是確認。
  - 其他 Agent 請在對話中詢問。
- 先確認目前不在 master（`git rev-parse --abbrev-ref HEAD`）。
- commit 訊息用繁體中文，第一行一句話說明做了什麼，例如「修正造訪他人頁面時替別人跳成就」。需要時在空一行後補充細節。
- 依各 Agent 的規定附上 Co-Authored-By 署名。

## 6. 開 PR

```
git push -u origin <分支>
gh pr create --base master --title "<標題>" --body-file <暫存檔>
```

- **標題**：繁體中文。對應 issue 時寫成「Issue #<編號>：<內容>」。
- **內文模板**：

```markdown
Closes #<編號>

## 摘要

<一段話：為什麼改、改了什麼>

## 變更

- **<區塊>**：<具體變更，提到重要的檔案或類別>

## 測試

- 建置成功
- `dotnet test`：<通過數>
- 瀏覽器：<淺色／深色／375 px 驗證了哪些頁面>
- 未驗證：<沒有驗證到的部分，誠實列出>

## 部署注意

<有 migration 時寫「部署時勾選 run_migration」；有新設定值時寫出設定名稱與預設值>
```

- 依各 Agent 的規定附上署名（例如「🤖 Generated with ...」）。

## 7. 開完 PR

- **只回報 PR 連結**。CI 跑得慢，開完 PR 時不要查 CI 狀態，也不要在回覆裡提 CI 還沒跑完。
- 不要自己合併 PR，也不要開啟自動合併，除非使用者要求。
