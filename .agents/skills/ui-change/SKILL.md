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

**從畫面找原始碼**：畫面上的文字可能是 Razor 組出來的（例如「30 天」在程式裡是 `@days 天`），直接搜尋文字會找不到。先用 `Tools/PageInventory` 找到頁面檔，再看它用到的 partial；或在瀏覽器看元素的 class，再搜尋 class。

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

### 修改範圍

- **只修使用者要求的問題**。檢查時看到其他既有的問題（例如旁邊的按鈕對比也不足），不要順手修，在回報時列出來讓使用者決定。順手修會讓 PR 變大，也可能改到使用者不想動的設計。
- **改到共用樣式**（`.btn-primary`、主題變數等）時，影響的是全站。回報時說明還有哪些地方跟著變，並至少再看一個用到它的頁面。
- **同一個問題在另一個主題也不合格時要先講**。例如深色模式的對比修好了，但淺色模式原本也只有 2.92：在回報的最前面說明，並問使用者要不要一起修，不要只報修好的那一半。

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
3. **改之前先量一次、截一張圖**：在要修的那個情況（例如深色模式）跑第 5 步的自動檢查並截圖，記下修改前的數字。少了這一步，就沒辦法證明是這次修好的，也分不出哪些問題原本就有。
4. **改完後檢查三種情況**，每種都跑自動檢查：

   | 情況 | 怎麼切換 |
   |---|---|
   | 淺色、桌面寬度 | `localStorage.setItem('theme', 'light'); location.reload();` |
   | 深色、桌面寬度 | `localStorage.setItem('theme', 'dark'); location.reload();` |
   | 手機寬度 375 px | Claude Code 用 `resize_window`（寬 375、高 812）；其他環境用 DevTools 的裝置模式。不能有水平捲動。改到的是顏色時，淺色與深色都要看 |

   - 網站預設的主題是 `auto`（跟著系統），所以淺色與深色都要明確指定，不要假設一開始是淺色。
   - 驗證完用 `localStorage.removeItem('theme')` 還原主題，並把視窗改回桌面大小。
5. **自動檢查**：第一次把 `Tools/UiAudit/ui-audit.js` 的完整內容交給瀏覽器執行。它會把自己存進 sessionStorage，同一個分頁之後（重新整理後也可以）只要執行：
   ```
   eval(sessionStorage.getItem('uiAudit'))
   ```
   - 回傳 `error`（viewport 是 0×0）代表瀏覽器面板被隱藏，量到的數字都不能用。先顯示面板或用 `resize_window` 設定大小，再重跑。
   - 正常時回傳水平捲動、缺 aria-label、觸控目標過小、缺 alt、對比不足的清單。新出現的問題要修掉；既有的問題依「修改範圍」處理。
6. **截圖**：修改後的畫面，和第 3 步的修改前一起給使用者看。
7. **前端規範檢查**（顏色寫死、屬性排序、單引號、`var`、import map）：
   ```
   cd Tools/FrontendLint
   dotnet run felint.cs
   ```
8. **收尾**：停掉 dev server、關閉瀏覽器分頁、執行 `dotnet build-server shutdown`。
