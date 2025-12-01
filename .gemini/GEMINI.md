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
  * 共用元件：共用的視圖元件 View Components 應放置在 `Views/Shared` 資料夾中。
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
  * HTML 渲染：要使用 `@Html.AntiXssRaw`。
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
  * 視圖元件 (View Components)：對於需要後端邏輯處理的、可重複使用的 UI 元件（例如，需要從資料庫取得資料的側邊欄、使用者選單、留言），應優先使用 View Component。這有助於將 UI 元件的邏輯與頁面本身分離，提高模組化與可測試性。

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
  * 長內容 String 應該用 `CustomHtmlSanitizer.SanitizeHtml` 消毒。
  * 路由前綴：所有 API Controller 路由應以 `/api` 作為統一前綴，以區分前端頁面路由。
  * 版本控制：在路由中加入版本號，例如 `/api/v1/...`，方便未來 API 的升級與管理。
  * 資源命名：使用複數名詞來表示資源集合，例如 `/api/v1/articles`, `/api/v1/users`。
  * HTTP 方法對應：
    * GET `/api/v1/articles`：獲取所有文章。
    * GET `/api/v1/articles/{id}`：獲取單篇文章。
    * POST `/api/v1/articles`：新增文章。
    * PUT `/api/v1/articles/{id}`：更新文章。
    * DELETE `/api/v1/articles/{id}`：刪除文章。

## 7. 單元測試

1.  測試類別命名：
    * 格式：`[待測類別名稱]Tests`
    * 範例：`StringExtensions` 的測試類別應命名為 `StringExtensionsTests`。

2.  測試方法命名：
    * 格式：`[測試方法名稱]_[情境]_[預期結果]`
    * 範例：
        * 測試 `DiceHelper` 的 `Roll` 方法在有效輸入下的成功情境：`Roll_WithValidInput_ReturnsSuccess`
        * 測試 `TimePeriodHelper` 的 `GetTimePeriod` 方法在輸入為 null 時的例外情境：`GetTimePeriod_WhenTimeIsNull_ThrowsArgumentNullException`

3.  Mock Data 放置位置：
    * 在 `InvolverTest` 專案下建立一個名為 `TestData` 的資料夾。
    * 在此資料夾中，根據不同的測試類別，再建立對應的子資料夾，用於存放相關的 mock data 檔案。
    * 範例：`InvolverTest/TestData/DiceHelperTests/valid-dice-rolls.json`

-----

## 8. 前端開發規範

  * CSS：
  
      * 使用 BEM (Block, Element, Modifier) 等命名規範來組織 CSS 類別。
      * 盡量使用 CSS 類別而非 ID 來選擇元素，以利於樣式覆寫與重用。
      * 屬性排序：為了提升可讀性，將每個區塊內的 CSS 屬性依照字母順序排列。
      * 結構：
          1.  根變數 (Root Variables)：定義了淺色與深色模式下的主題色彩。
          2.  基礎與全域樣式 (Base & Global Styles)：設定 `html`、`body` 的基本樣式。
          3.  佈局 (Layout)：包含頁尾、背景圖片等區塊。
          4.  導覽列 (Navbar)：所有與 Navbar 相關的樣式。
          5.  卡片 (Cards)：卡片的背景、圖片與互動效果。
          6.  按鈕 (Buttons)：主要按鈕、社群媒體按鈕及連結按鈕的樣式。
          7.  元件 (Components)：對 Bootstrap 元件的樣式覆寫，如分頁、下拉選單、提示等。
          8.  自訂功能樣式 (Custom Feature Styles)：針對特定功能的樣式，如留言區、擲骰、標籤雲等。
          9.  通用類別 (Utility Classes)：功能性的輔助類別，如邊框、陰影、漸層背景等。
          10. 動畫 (Animations)：Keyframes 動畫定義。

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
            async function fetchNovelDetailsAsync(novelId) {
              // ...
            }
            ```
      * 模組化：
          * 一律使用 ES module (`import`/`export`)。
      * 位置：
          * 頁面專屬的 JavaScript 應放在頁面底部，以避免阻擋頁面渲染。
		  * 功能共用的 JavaScript 應寫在 `wwwroot\js\site.js` 裡。

  * JS Function 命名：
    *  非同步函式：所有非同步函式，特別是與 API 互動的函式，應以 `async` 關鍵字開頭，並在結尾加上 `Async` 後綴，以清楚表明其非同步特性。
        * 範例：`async function getUserProfileAsync(userId) { ... }`

    *  API 呼叫函式：
        * GET：以 `get` 開頭，例如 `getUserProfile`。
        * POST：以 `create` 或 `add` 開頭，例如 `createNewArticle`。
        * PUT/PATCH：以 `update` 開頭，例如 `updateUserProfile`。
        * DELETE：以 `delete` 或 `remove` 開頭，例如 `deleteComment`。

  * Vue.js 開發規範 (Options API)
    * 模組化語法：
        * 一律使用 `import { createApp } from 'vue'` 開頭。
        * 禁止依賴全域變數 (如 `window.Vue`)。
    * 初始化與掛載：
        * 每個 View Component 對應一個獨立的 Vue App 實體。
        * 在 JS 檔末尾執行 `app.mount('#component-id')`。
    * 資料傳遞 (C# -> Vue)：
        * 推薦方式：使用 HTML `data-*` 屬性 (Dataset) 傳遞初始化設定與簡單資料。
        * 讀取時機：在 Vue 的 `mounted()` 生命週期中，透過 `document.getElementById(...).dataset` 讀取並轉型。
    * Razor 語法衝突處理：
        * Vue 的事件綁定 `@click` 在 Razor 檔案中必須寫成 `@@click` 以進行跳脫。
        * Vue 的動態參數 `:id` 或 `v-bind:id` 可直接使用，不會衝突。
    * 程式碼風格與順序 (Options API)
    為保持一致性，Vue 實體內的屬性順序如下：
        1. `data()`: 必須是函式，回傳元件狀態。
        2. `watch`: 監聽資料變動。
        3. `computed`: 計算屬性。
        4. `mounted()`: 執行 DOM 相關初始化 (讀取 dataset、綁定編輯器等)。
        5. `methods`: 業務邏輯函式。
            * API 呼叫方法建議使用 `Async` 結尾 (如 `getCommentsAsync`)。
            * 複雜邏輯應加上 JSDoc 註解。
  * Vue.js + View Components + Import Maps
    * Razor Pages 作為主要架構，在互動性較高的區塊（如留言板、即時更新區）使用 Vue.js 進行增強。使用 Import Maps 技術直接在瀏覽器中載入 ES Modules，無需額外的前端打包工具 (Webpack/Vite)。
    * 檔案結構：採用 View Components 封裝 UI 與邏輯，將 C# 後端邏輯、HTML 結構與 Vue 前端互動邏輯分離。
  	    * 後端邏輯 (C# ViewComponent)
            * 位置：`ViewComponents/` 或 `Views/Shared/Components/{ComponentName}/`
            * 職責：處理資料庫查詢、權限驗證，並建立 ViewModel。
            * 範例：`CommentSectionViewComponent.cs`
        * 視圖樣板 (Razor View)
            * 位置：`Views/Shared/Components/{ComponentName}/Default.cshtml`
            * 職責：
                1. 定義 Vue 的掛載點 (Mount Point) 與 HTML 結構。
                2. 利用 Razor 語法 (`@Model`) 將後端資料轉換為 `data-*` 屬性 (Dataset)。
                3. 引用對應的 JS 模組 (`<script type="module" src="...">`)。
        * 前端邏輯 (Vue ES Module)
            * 位置：`wwwroot/js/components/{component-name}.js` (或依功能分類)
            * 職責：定義 Vue 實體、處理使用者互動、呼叫 API。
            * 範例：`comments.js`

## 9. 相容性與相依性

  * .NET 版本：以 .Net 8 規格撰寫 C# 程式碼。
  * Bootstrap 版本：v5.3.8。
  * 外部相依性：除非絕對必要，否則避免引入新的外部相依性。若有需要，必須在團隊中提出討論並說明原因。
