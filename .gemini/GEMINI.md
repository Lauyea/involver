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
    * **GET `/api/v1/articles`**：獲取所有文章。
    * **GET `/api/v1/articles/{id}`**：獲取單篇文章。
    * **POST `/api/v1/articles`**：新增文章。
    * **PUT `/api/v1/articles/{id}`**：更新文章。
    * **DELETE `/api/v1/articles/{id}`**：刪除文章。

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
        * **GET**：以 `get` 開頭，例如 `getUserProfile`。
        * **POST**：以 `create` 或 `add` 開頭，例如 `createNewArticle`。
        * **PUT/PATCH**：以 `update` 開頭，例如 `updateUserProfile`。
        * **DELETE**：以 `delete` 或 `remove` 開頭，例如 `deleteComment`。
		  
  * jQuery (使用指南)：

      * 優先使用：對於簡單的 DOM 操作、事件處理、以及 AJAX 請求，應優先使用 jQuery 來處理。
      * 複雜互動：如果頁面需要複雜的狀態管理或大量的雙向資料綁定，才應考慮導入 Vue.js。
      * 命名：儲存 jQuery 物件的變數，應以 `$` 符號開頭，例如 `const $modal = $('#myModal');`。
      * DOM Ready：所有的 jQuery 程式碼都應放在 `$(function() { ... });` 區塊中，確保在 DOM 完全載入後才執行。
      * 鏈式呼叫 (Chaining)：盡可能使用鏈式呼叫來對同一元素執行多個操作，以增加程式碼的簡潔性和可讀性。
        ```javascript
        // 推薦
        $('#myElement')
            .addClass('active')
            .css('color', 'red')
            .show();
        ```
      * 選擇器 (Selectors)：盡量使用具體且高效的選擇器。優先使用 ID 選擇器 (`$('#myId')`)，其次是 class 選擇器 (`$('.myClass')`)。避免使用過於籠統的標籤選擇器。

  * Vue.js (未來導入規範 - Options API 風格)
    * 檔案結構：
  	  * 元件檔案應放置在 `Components` 資料夾中，並依功能或頁面進行分類。
  	  * 每個元件應為一個 `.vue` 單一檔案元件 (Single File Component)。
    * 命名：
  	  * 元件檔名：使用 `PascalCase` (例如: `NovelCard.vue`)。
  	  * 元件在模板中使用：使用 `<kebab-case>` (例如: `<novel-card>`)。
    * 元件結構 (Options API)：
  	  * 為確保程式碼的一致性與可讀性，元件內的選項 (Options) 應遵循以下建議順序。
  	  * 建議順序：
  		1.  `name`: 元件名稱，應與檔名保持一致的 `PascalCase`，有助於除錯。
  		2.  `components`: 註冊此元件所使用的子元件。
  		3.  `props`: 定義從父元件接收的屬性。
  		4.  `emits`: 聲明此元件可以發出的自訂事件，以利於溝通。
  		5.  `data`: 管理元件的內部響應式狀態，必須是一個函式 (`function`)。
  		6.  `computed`: 計算屬性，用於衍生出新的狀態。
  		7.  `watch`: 監聽器，用於觀察資料變化並執行相應操作。
  		8.  生命週期鉤子 (Lifecycle Hooks)：按照執行的順序排列 (例如: `created`, `mounted`, `updated`, `unmounted`)。
  		9.  `methods`: 方法，放置元件的業務邏輯函式。
  	  * 範例：
  		```javascript
  		<script>
  		import AuthorTag from './AuthorTag.vue';
  
  		export default {
  		  name: 'NovelCard',
  		  components: {
  			AuthorTag,
  		  },
  		  props: {
  			novel: {
  			  type: Object,
  			  required: true,
  			},
  		  },
  		  emits: ['add-to-favorite'],
  		  data() {
  			return {
  			  isFavorite: false,
  			  userComment: '',
  			};
  		  },
  		  computed: {
  			displayTitle() {
  			  return `《${this.novel.title}》`;
  			},
  		  },
  		  watch: {
  			isFavorite(newValue) {
  			  console.log(`小說 ${this.novel.title} 的收藏狀態變更為: ${newValue}`);
  			}
  		  },
  		  mounted() {
  			// DOM 掛載後執行的操作
  			console.log('NovelCard component has been mounted.');
  		  },
  		  methods: {
  			toggleFavorite() {
  			  this.isFavorite = !this.isFavorite;
  			  if (this.isFavorite) {
  				this.$emit('add-to-favorite', this.novel.id);
  			  }
  			},
  		  },
  		};
  		</script>
  		```
    * 開發原則：
  	  * 單向數據流：嚴格遵守 `props` 向下傳遞 (`props` down)，事件向上發出 (`events` up) 的原則。子元件不應直接修改父元件傳入的 `props`。
  	  * 元件職責：保持元件的單一職責，避免建立過於龐大且複雜的元件。
  	  * 狀態管理：對於跨多個元件共享的狀態，應考慮使用 Pinia 或類似的狀態管理工具。

## 9. 相容性與相依性

  * .NET 版本：以 .Net 8 規格撰寫 C# 程式碼。
  * Bootstrap 版本：v4.3.1。
  * 外部相依性：除非絕對必要，否則避免引入新的外部相依性。若有需要，必須在團隊中提出討論並說明原因。
