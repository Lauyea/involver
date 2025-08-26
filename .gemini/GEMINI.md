# Involver.tw 程式碼風格與開發指南

目標： 確保程式碼清晰、易讀、一致，提升團隊協作效率，並利於 AI 夥伴（如 Copilot、Gemini CLI）學習與遵循。

## 1. 檔案與資料夾結構

  * 頁面檔案：每個頁面應由 `.cshtml` 和對應的 `.cshtml.cs` 檔案組成。
  * 頁面分組：依據功能或業務邏輯將頁面組織在 `Pages` 資料夾下的子資料夾中。
      * 範例：
        ```
        /Pages
        ├── Articles
        │   ├── Create.cshtml
        │   ├── Create.cshtml.cs
        │   ├── Index.cshtml
        │   └── Index.cshtml.cs
        └── Novels
             ├── Create.cshtml
             ├── Create.cshtml.cs
             ├── Index.cshtml
             └── Index.cshtml.cs
        ```
  * 帳號管理：相關頁面應放在 `Areas/Identity` 資料夾中。
  * 共用視圖：共用的頁面區塊（如導覽列、頁尾）應放置在 `Pages/Shared` 資料夾中。
  * 模型 (Models)：
      * 強型別模型：應放在 `Models` 資料夾中，與頁面模型 `PageModel` 區分開來。
      * ViewModels：可以建立一個 `Models/ViewModels` 資料夾，專門存放為特定 View 所設計的資料模型。這有助於區分資料庫實體模型和純粹為 UI 顯示服務的模型。

## 2. C# 程式碼風格 (後端邏輯)

  * 核心原則：資料處理優先在 C# 完成。盡量將所有資料的查詢、篩選、排序、計算等邏輯放在 PageModel 中處理。避免在 Razor 或 JavaScript 中執行複雜的業務邏輯。

  * 命名：

      * 類別名稱 (PageModel, Model)：使用 `PascalCase`，例如 `ArticleModel`。
      * 方法名稱：使用 `PascalCase`，例如 `OnGetAsync`。
      * 私有變數：使用 `_` 開頭的 `camelCase`，例如 `_context`。
      * 公有屬性：使用 `PascalCase`，例如 `public Article Article { get; set; }`。

  * 方法：

      * `OnGet` 或 `OnPost` 方法應保持精簡，主要用於處理資料獲取、綁定和重導向。
      * 所有非同步方法都應該以 `Async` 結尾。

  * 屬性：

      * 使用 `[BindProperty]` 屬性來綁定表單提交的資料。
      * 使用 `[TempData]` 來在不同請求間傳遞臨時資料。
      * 應避免使用 `ViewData` 和 `ViewBag`，優先使用強型別模型。

  * 註解：確保所有新的函式、類別、方法以及 Model 參數都有清楚的 XML 註解。

## 3. Razor 標記 (在 `.cshtml` 檔案中)

  * 頁面模型：每個頁面的頂部應定義強型別模型：`@model involver.Pages.Articles.IndexModel`。
  * 程式碼區塊：使用 `@` 符號來標記程式碼。盡量保持 Razor 檔案的簡潔，避免複雜的 C# 邏輯。
  * 標籤協助器 (Tag Helpers)：優先使用標籤協助器來替代傳統的 HTML 輔助方法，以增加程式碼的可讀性。
      * 範例：
        ```html
        <form method="post">
            <input asp-for="Article.Title" />
            <span asp-validation-for="Article.Title"></span>
            <button type="submit">送出</button>
        </form>
        ```
  * 部分視圖 (Partial Views)：對於重複出現的 UI 元件（如小說卡片、作者簡介區塊），應建立成部分視圖來提高重用性。

## 4. 資料存取

  * 非同步操作：一律使用 `async/await` 搭配 `CancellationToken`。
  * EF Core 查詢：
      * 對於唯讀操作，使用 `AsNoTracking()` 以提升效能。
      * 查詢語句應分行對齊，提升可讀性。
      * 範例：
        ```csharp
        var items = await _context.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        ```
  * 開發模式：使用 Code First 的方式進行開發。

## 5. API 設計

  * RESTful 風格：API 端點應遵循 RESTful 設計原則，使用 `GET`, `POST`, `PUT`, `DELETE` 等 HTTP 方法來對應資源的操作。

-----

## 6. 前端開發規範

  * CSS：

      * 使用 BEM (Block, Element, Modifier) 等命名規範來組織 CSS 類別。
      * 盡量使用 CSS 類別而非 ID 來選擇元素，以利於樣式覆寫與重用。

  * JavaScript (通用規範)：

      * 語法：
          * 使用 ES6+ 語法 (`let`, `const`, `class`, `arrow function`, `async/await`)。
          * 優先使用 `const`，除非變數需要被重新賦值才用 `let`。嚴禁使用 `var`。
          * 優先使用箭頭函式 `=>`，特別是在回呼函數中。
          * 使用模板字面量 (`` ` ``) 進行字串拼接。
          * 使用嚴格相等運算子 `===` / `!==`。
      * 格式：
          * 使用 2 個空格 進行縮排，不使用 Tab。
          * 每行陳述式結尾必須使用分號 `;`。
          * 字串一律使用單引號 `'`。
      * 命名：
          * 變數與函式：使用 `camelCase` (例如: `novelCount`)。
          * 類別與建構函式：使用 `PascalCase` (例如: `NovelService`)。
          * 常數：使用 `UPPER_SNAKE_CASE` (例如: `MAX_LIMIT`)。
      * 註解：
          * 公開的函數和類別需使用 JSDoc 格式註解，說明其用途、參數 (`@param`) 和返回值 (`@returns`)。
            ```javascript
            /
             * 根據小說ID取得小說詳細資訊
             * @param {string} novelId - 小說的唯一ID
             * @returns {Promise<Object>} 包含小說標題和內容的物件
             */
            async function fetchNovelDetails(novelId) {
              // ...
            }
            ```
      * 模組化：
          * 一律使用 ES module (`import`/`export`)。
      * 位置：
          * 頁面專屬的 JavaScript 應放在頁面底部，以避免阻擋頁面渲染。

  * Vue.js (未來導入規範)：

      * 檔案結構：
          * 元件檔案應放置在 `Components` 資料夾中，並依功能或頁面進行分類。
          * 每個元件應為一個 `.vue` 單一檔案元件 (Single File Component)。
      * 命名：
          * 元件檔名：使用 `PascalCase` (例如: `NovelCard.vue`)。
          * 元件在模板中使用：使用 `<kebab-case>` (例如: `<novel-card>`)。
      * 開發原則：
          * 單向數據流：嚴格遵守 `props` down, `events` up 的原則，子元件不應直接修改父元件傳入的 `props`。
          * 元件職責：保持元件的單一職責，避免建立過於龐大且複雜的元件。
          * 狀態管理：對於跨多個元件共享的狀態，應考慮使用 Pinia 或類似的狀態管理工具。

## 7. 相容性與相依性

  * .NET 版本：以 .Net 8 規格撰寫 C# 程式碼。
  * Bootstrap 版本：v4.3.1。
  * 外部相依性：除非絕對必要，否則避免引入新的外部相依性。若有需要，必須在團隊中提出討論並說明原因。
