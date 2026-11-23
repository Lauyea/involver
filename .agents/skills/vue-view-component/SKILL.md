---
name: vue-view-component
description: 在 Involver 新增或修改用 Vue 3 增強互動的 View Component（例如留言、投票、追蹤按鈕、預覽、標籤輸入），或把頁面上的 onclick／全域 JS 函式改寫成 Vue 元件。涵蓋 C# ViewComponent＋ViewModel、Default.cshtml 掛載點與 data-* 傳值、wwwroot/js/components 的 ES module、import map、Razor 的 @@click 跳脫、用 /js/api-client.js 呼叫 API 與 anti-forgery（X-CSRF-TOKEN）、未登入的處理。Use when building an interactive widget with Vue in a Razor page, or touching Views/Shared/Components or wwwroot/js/components.
---

# Vue View Component

架構：Razor Pages 為主；互動性高的區塊用 **View Component＋Vue 3（Options API）**。Vue 透過 import map 以 ES module 載入，沒有打包工具。

現有範例：`Voting`、`CommentSection`（`Involver/Views/Shared/Components/`）對應 `voting.js`、`comments.js`（`Involver/wwwroot/js/components/`）。結構可以參考，但**呼叫 API 的寫法不要照抄它們**：它們早於 `api-client.js`，沒有帶 anti-forgery token。

## 開始前先看清楚現況

改寫既有功能（例如把 `onclick="FollowNovel(...)"` 換成元件）時，先讀現在的 JS 與它呼叫的 controller，再決定要保留什麼：

- **會改資料的 GET 要改掉**。例如用 `[HttpGet]` 切換狀態的 action：任何連結、圖片或爬蟲都能觸發，也沒有 CSRF 保護。改成 `api/v1/<複數資源>` 的 POST／PUT／DELETE，並加上 `[ValidateAntiForgeryToken]`。
- 舊的 action 或 JS 檔用 grep 確認沒有其他呼叫者後才刪除；有就保留，並在回報說明。
- 同一頁上其他由伺服器算好的數字（例如統計列的追蹤數），元件更新後不會跟著變。要嘛一起交給 Vue，要嘛在回報說明。

## 檔案

元件名稱以 `{Name}` 表示（下面的範例用 `Foo`）：

| 檔案 | 職責 |
|---|---|
| `Involver/Views/Shared/Components/{Name}/{Name}ViewComponent.cs` | 查資料、判斷權限、建立 ViewModel |
| `Involver/Views/Shared/Components/{Name}/Default.cshtml` | Vue 掛載點與 HTML 模板，用 `data-*` 帶入初始資料，最後引用 JS |
| `Involver/wwwroot/js/components/{name}.js` | Vue app：互動與 API 呼叫 |
| `Involver/Controllers/{Name}sApiController.cs`（需要時） | REST API，路由 `api/v1/<複數資源>` |
| `InvolverTest/Controllers/{Name}sApiControllerTests.cs`（有 API 時） | controller 單元測試 |

在頁面使用：`@await Component.InvokeAsync("Foo", new { contentId = Model.X })`。

## C#：ViewComponent

```csharp
namespace Involver.Views.Shared.Components.Foo;

/// <summary>
/// Foo 區塊：…
/// </summary>
public class FooViewComponent(ApplicationDbContext context, UserManager<InvolverUser> userManager) : ViewComponent
{
    /// <summary>
    /// 產生 Foo 區塊
    /// </summary>
    /// <param name="contentId">…</param>
    public async Task<IViewComponentResult> InvokeAsync(int contentId)
    {
        var userId = userManager.GetUserId(UserClaimsPrincipal);
        var count = await context.Things
            .Where(t => t.ContentId == contentId)
            .AsNoTracking()
            .CountAsync(HttpContext.RequestAborted);
        return View(new FooViewModel { ContentId = contentId, IsSignedIn = userId != null, Count = count });
    }
}
```

- ViewModel 放在同一個資料夾，或放在 `Involver/Models/ViewModels/`。每個屬性都要有 XML 註解。
- 唯讀查詢用 `AsNoTracking()`，並傳入 `CancellationToken`。
- 元件要自己判斷權限（是否登入、是否為本人），不要依賴呼叫它的頁面先擋掉。

## C#：API controller

```csharp
/// <summary>
/// Foo 的 API
/// </summary>
[Route("api/v1/foos")]
[ApiController]
[ValidateAntiForgeryToken]
public class FoosApiController(ApplicationDbContext context, UserManager<InvolverUser> userManager) : ControllerBase
{
    /// <summary>
    /// 更新 Foo，回傳更新後的狀態讓前端直接顯示
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, CancellationToken cancellationToken) { /* … */ return Ok(new { isOn, count }); }
}
```

- `Program.cs` 對所有 controller 加了「必須登入」的全域 filter（不需要另外寫 `[Authorize]`；要開放匿名才加 `[AllowAnonymous]`）。未登入時，**只有 `/api` 開頭的路由會回 401**；其他路由會被轉址到登入頁，`fetch` 拿到的是 200 的 HTML，前端分辨不出失敗。這也是 API 要放在 `api/v1/` 的原因之一。
- 回傳更新後的狀態（例如 `{ followed, followerCount }`），前端用伺服器的值更新畫面，不要自己加減。
- 讓操作可以重複呼叫（PUT 已追蹤、DELETE 已取消都回成功），避免連點出錯。
- 長內容字串用 `CustomHtmlSanitizer.SanitizeHtml` 消毒。

## Razor：Default.cshtml

```cshtml
@model Involver.Views.Shared.Components.Foo.FooViewModel

<div class="foo" data-foo
     data-content-id="@Model.ContentId"
     data-signed-in="@Model.IsSignedIn.ToString().ToLower()"
     data-count="@Model.Count"
     v-cloak>
    @Html.AntiForgeryToken()
    <button type="button" class="btn btn-primary" @@click="updateFooAsync" :disabled="isSaving">儲存</button>
    <small aria-live="polite">{{ count }}</small>
    <small v-if="error" class="text-danger" role="alert">{{ error }}</small>
</div>

<script src="~/js/components/foo.js" type="module" asp-append-version="true"></script>
```

- Vue 的事件在 Razor 裡要寫成 **`@@click`**、`@@submit.prevent`。`:prop` 和 `v-bind` 可以直接寫。
- 用 `data-foo` 這類屬性當掛載目標，不要用固定 id：同一頁放多個實例（例如列表裡每一列一個按鈕）也能用。
- 在元件裡放 `@Html.AntiForgeryToken()`，不必依賴頁面剛好有 `<form method="post">`。
- `v-cloak` 讓掛載前不會閃出 `{{ }}`，要在 `site.css` 加上 `.foo[v-cloak] { visibility: hidden; }`（參考 `.bracket-builder[v-cloak]`）。
- 模板寫在 .cshtml 裡，由瀏覽器編譯（in-DOM template）：有子元素的標籤不要用 `v-text`／`v-html`，標籤名稱與屬性要小寫。編譯錯誤會出現在 console。
- 使用者輸入的 HTML 一律在伺服器端消毒。在 Vue 裡用 `v-html` 顯示的內容必須是消毒過的。

## JS：foo.js

```javascript
import { createApp } from 'vue';
import { sendJsonAsync } from '/js/api-client.js';

/**
 * 掛載頁面上所有 Foo 元件，每個元素是獨立的 Vue app
 */
document.querySelectorAll('[data-foo]:not([data-v-app])').forEach((el) => {
  createApp({
    data() {
      return { contentId: 0, isSignedIn: false, count: 0, isSaving: false, error: null };
    },
    watch: {},
    computed: {},
    mounted() {
      this.contentId = Number.parseInt(el.dataset.contentId, 10);
      this.isSignedIn = el.dataset.signedIn === 'true';
      this.count = Number.parseInt(el.dataset.count, 10);
    },
    methods: {
      /**
       * 儲存變更，以伺服器回傳的狀態更新畫面
       * @returns {Promise<void>}
       */
      async updateFooAsync() {
        if (!this.isSignedIn) {
          window.location.href = `/Identity/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
          return;
        }
        this.isSaving = true;
        this.error = null;
        try {
          const response = await sendJsonAsync(`/api/v1/foos/${this.contentId}`, 'PUT');
          const result = await response.json();
          this.count = result.count;
        } catch (e) {
          this.error = '儲存失敗，請稍後再試';
        } finally {
          this.isSaving = false;
        }
      },
    },
  }).mount(el);
});
```

- **呼叫 API 一律用 `/js/api-client.js` 的 `sendJsonAsync`**（已在 import map 裡）。它會帶上 anti-forgery token，header 名稱是 `Program.cs` 設定的 **`X-CSRF-TOKEN`**；自己寫 `fetch` 時 header 名稱錯了，伺服器會回 400。失敗時它會 throw。
- 一定要 `import { createApp } from 'vue'`，不能用 `window.Vue`。
- 屬性順序固定：`data` → `watch` → `computed` → `mounted` → `methods`。初始資料在 `mounted()` 從 `el.dataset` 讀。
- 呼叫 API 的方法以 `Async` 結尾，命名用 get／create／update／delete 開頭。
- 錯誤顯示在元件裡（`role="alert"`），不要用 `alert()`。
- 程式碼風格：`const`／`let`、箭頭函式、單引號、分號、`===`。

## 未登入的使用者

明確決定未登入時要看到什麼，並寫在回報裡。通常是**照樣顯示按鈕與數字，點擊時導向登入頁**（帶 `returnUrl`），讓訪客知道有這個功能；只把按鈕藏起來，訪客就不知道可以追蹤或投票。API 回 401 時也導向登入頁，不要無聲失敗。

## 新的第三方 ES module

1. 用 LibMan 加進 `Involver/libman.json`（見 `dependency-upgrade` skill）。
2. 在 `Involver/Helpers/ImportMapHelper.cs` 加入對應的 specifier。
3. JS 裡用 bare specifier 匯入。

## 驗證

1. `Tools/FrontendLint`：檢查 `var`、雙引號、`window.Vue`，以及 import 有沒有在 import map 裡。
2. `dotnet build`；有 API 時補上 controller 單元測試（`InvolverTest/Controllers/`，資料庫用 `InvolverTest/Infrastructure/SqliteTestDatabase`），再跑 `dotnet test`（或 `Tools/AffectedTests` 並說明）。
3. 在瀏覽器實際操作（啟動與登入見 `run-involver` skill）：
   - 用測試帳號登入，每個用到元件的頁面都點一次，確認畫面與資料庫都更新，再點一次還原。
   - 登出後點一次，確認導向登入頁。
   - 看 console 沒有錯誤（Vue 模板編譯錯誤會出現在這裡）。
   - 依 `ui-change` 確認淺色、深色、375 px。
4. 驗證時改到的資料要還原，結束時停掉網站（見 `run-involver`）。
