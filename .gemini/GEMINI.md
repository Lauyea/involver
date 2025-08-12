Razor Pages 程式碼風格指南 (Code Style Guide)

1. 檔案與資料夾結構

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
  * 帳號管理：在`Areas/Identity`資料夾中
  * 共用視圖：共用的頁面區塊（如導覽列、頁尾）應放置在 `Pages/Shared` 資料夾中。
  * 強型別模型：強型別模型應放在 `Models` 資料夾中，與頁面模型 `PageModel` 區分開來。
  * ViewModels 資料夾：可以建立一個 `Models/ViewModels` 資料夾，專門存放為特定 View 所設計的資料模型。這有助於區分資料庫實體模型 (`Models`) 和純粹為 UI 顯示服務的模型 (`ViewModels`)。

2. C# 程式碼風格

  * 命名：
      * 類別名稱（PageModel、Model）：使用 PascalCase，例如 `ArticleModel`。
      * 方法名稱（`OnGet`、`OnPostAsync`）：使用 PascalCase。
      * 私有變數：使用 `_` 開頭的 camelCase，例如 `_context`。
      * 公有屬性：使用 PascalCase，例如 `Article`。
  * 方法：
      * `OnGet` 或 `OnPost` 方法應保持盡可能精簡，主要用於處理資料獲取、綁定和重導向。
      * 所有非同步方法都應該以 `Async` 結尾。
      * 避免在 `.cshtml` 中包含複雜的業務邏輯，應移至 PageModel 或將其委託給服務層 (Service Layer)。
  * 屬性：
      * 使用 `[BindProperty]` 屬性來綁定表單提交的資料。
      * 使用 `[TempData]` 來在不同請求間傳遞資料。
      * 應避免使用 `ViewData` 和 `ViewBag`，優先使用強型別模型。
  * 註解：確保所有新的函式、類別、方法以及Model parameters都有註解。

3. Razor 標記 (在 `.cshtml` 檔案中)

  * 頁面模型：每個頁面的頂部應定義強型別模型：`@model involver.Pages.Articles.IndexModel`。
  * 程式碼區塊：使用 `@` 符號來標記程式碼。
      * 行內表達式：`@DateTime.Now`
      * 程式碼區塊：
        ```csharp
        @{
            var title = "Hello World";
        }
        ```
  * 條件與迴圈：
      * `if` 語句：
        ```csharp
        @if (Model.Articles.Count > 0)
        {
            <p>有文章</p>
        }
        ```
      * `foreach` 迴圈：
        ```csharp
        @foreach (var article in Model.Articles)
        {
            <li>@article.Title</li>
        }
        ```
  * 標籤協助器 (Tag Helpers)：
      * 優先使用標籤協助器來替代傳統的 HTML 輔助方法。
      * 範例：
        ```html
        <form method="post">
            <input asp-for="Article.Title" />
            <span asp-validation-for="Article.Title"></span>
            <button type="submit">送出</button>
        </form>
        ```
  * 部分視圖 (Partial Views)：對於重複出現的 UI 元件（如小說卡片、作者簡介區塊），應建立成部分視圖來提高重用性。

4. UI/UX 相關

  * CSS：
      * 使用 BEM (Block, Element, Modifier) 等命名規範來組織 CSS 類別。
      * 盡量使用 CSS 類別而非 ID 來選擇元素。
  * JavaScript：
      * 頁面專屬的 JavaScript 應放在頁面底部，以避免阻擋頁面渲染。

5. 資料存取

  * 使用 `async/await` 搭配 `CancellationToken`。
  * EF Core 查詢：
  
    * 使用 `AsNoTracking()` 讀取不需追蹤的資料。
    * 查詢語句分行對齊，提升可讀性。
    * 例：
  
      ```csharp
      var items = await _context.Posts
          .Where(p => p.IsPublished)
          .OrderByDescending(p => p.CreatedAt)
          .ToListAsync(cancellationToken);
      ```

6. 相容性

  * 以 .Net 8 規格撰寫 C# 程式碼。
  * Bootstrap 版本為 v4.3.1。

7. 相依性
  * 除非絕對必要，否則避免引入新的外部相依性。
  * 如果需要新的相依性，請說明原因。

8. API設計
  * RESTful 風格：API 端點應遵循 RESTful 設計原則，使用 `GET`, `POST`, `PUT`, `DELETE` 等 HTTP 方法來對應資源的操作。
