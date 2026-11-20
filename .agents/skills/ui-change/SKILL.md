---
name: ui-change
description: Involver 的 UI 修改流程：改 .cshtml 版面、site.css／feed.css／home.css、Bootstrap 元件、Material Design 3 風格、深色模式、手機版、無障礙時使用。涵蓋先找現有共用元件、CSS 規範（主題變數、BEM、屬性字母排序）、Modern Web Guidance 與 Bootstrap 的取捨，以及淺色／深色／375 px 的瀏覽器驗證與自動檢查。Use for any visual or layout change to Razor pages, partials, view components or CSS.
---

# UI 修改

設計原則參考 Material Design 3，實作沿用 Bootstrap 5.3 與 `site.css` 的主題變數（細節見 AGENTS.md「前端開發規範」）。這個 skill 是動手的順序與驗證方法。

## 1. 先找現有的元件

新增之前，先搜尋有沒有可以直接用或擴充的元件：

| 需求 | 現有元件 |
|---|---|
| 分頁標籤 | `Involver/Pages/Shared/_FeedTabsPartial.cshtml` |
| 篩選 chip | `.category-chip`（`_CategoryPickerPartial.cshtml`） |
| 作品卡片 | `Involver/Pages/Shared/_ContentCardPartial.cshtml` |
| 空狀態 | `.feed-empty`（圖示＋一句說明＋下一步按鈕） |
| 需要後端資料的區塊 | `Involver/Views/Shared/Components/*`（View Component）；要加 Vue 互動時用 `vue-view-component` skill |

搜尋方式：`grep -rn "class=\"<關鍵字>" Involver/Pages Involver/Views`，以及 `Involver/wwwroot/css/*.css`。

## 2. 寫法

- **顏色**一律用變數：`var(--primary-accent)`、`--card-bg-color`、`--border-color`、`--text-color` 等。變數定義在 `site.css` 開頭；深色模式的覆寫寫在 `[data-bs-theme="dark"]` 區塊。新顏色先加變數，淺色與深色都要給值。
- **CSS 類別**用 BEM（`.feed-card__title--muted`），不要用 ID 選擇器。每個區塊內的屬性**依字母排序**，並放進 `site.css` 對應的章節（章節順序見 AGENTS.md）。
- **`<nav>`**：`site.css` 會給所有 `<nav>` 加上導覽列的漸層與圓角，新的 `<nav>` 元件要自己覆寫。
- **Razor**：用 Tag Helper；輸出 HTML 用 `@Html.AntiXssRaw`；資料查詢與計算放在 PageModel，不要寫在 .cshtml 裡。
- **無障礙**：
  - 焦點要看得見（`:focus-visible`）
  - 觸控目標至少 40～48 px
  - 只有圖示的按鈕要有 `aria-label`
  - 圖片要有 `alt`
  - 淺色與深色模式的對比都要足夠
- **間距**以 4／8 px 為單位（Bootstrap 的 `gap-2`、`p-3` 等）。狀態層：滑鼠移過約 8%、焦點與按下約 12%。

## 3. Modern Web Guidance

做 dialog、popover、表單驗證、版面、動畫、效能、無障礙時，先用 `modern-web-guidance` skill 查最新做法。和 Bootstrap 衝突時依 AGENTS.md 的優先順序：

- **Bootstrap 已有的元件**（modal、dropdown、tooltip、collapse、offcanvas），沿用 Bootstrap。
- **Bootstrap 沒有的功能**，才採用原生 API 或 CSS。
- 目標是 **Baseline Widely available**。比這個新的功能要依 guide 提供 fallback；不引入 npm 套件或 polyfill 套件。

## 4. 驗證（每次修改都要做）

1. **啟動網站並登入**：用 `run-involver` skill。要測哪個身分，就用 `Tools/DevSeed` 的測試帳號。
2. **找頁面網址與需要的身分**：
   ```
   cd Tools/PageInventory
   dotnet run pages.cs -- --filter <關鍵字>
   ```
3. **三種情況各檢查一次**：
   - 淺色模式、桌面寬度
   - 深色模式，切換方式：`localStorage.setItem('theme', 'dark'); location.reload();`
   - 手機寬度 375 px，不能有水平捲動
4. **自動檢查**：每種情況都在瀏覽器執行 `Tools/UiAudit/ui-audit.js` 的完整內容，回傳水平捲動、缺 aria-label、觸控目標過小、缺 alt、對比不足的清單。新出現的問題要修掉；既有的問題在回報時說明即可。
5. **截圖**：給使用者看修改前後，或至少修改後的畫面。
6. **前端規範檢查**（顏色寫死、屬性排序、單引號、`var`、import map）：
   ```
   cd Tools/FrontendLint
   dotnet run felint.cs
   ```
7. **收尾**：停掉 dev server、關閉瀏覽器分頁、執行 `dotnet build-server shutdown`。
