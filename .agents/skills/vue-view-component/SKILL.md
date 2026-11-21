---
name: vue-view-component
description: 在 Involver 新增或修改用 Vue 3 增強互動的 View Component（例如留言、投票、預覽、標籤輸入）。涵蓋 C# ViewComponent＋ViewModel、Default.cshtml 掛載點與 data-* 傳值、wwwroot/js/components 的 ES module、import map、Razor 的 @@click 跳脫、API 呼叫與 CSRF token。Use when building an interactive widget with Vue in a Razor page, or touching Views/Shared/Components or wwwroot/js/components.
---

# Vue View Component

架構：Razor Pages 為主；互動性高的區塊用 **View Component＋Vue 3（Options API）**。Vue 透過 import map 以 ES module 載入，沒有打包工具。

現有範例可以照抄：`Voting`、`CommentSection`（`Involver/Views/Shared/Components/`）對應 `voting.js`、`comments.js`（`Involver/wwwroot/js/components/`）。

## 檔案

元件名稱以 `{Name}` 表示（下面的程式碼範例用 `Foo`）：

| 檔案 | 職責 |
|---|---|
| `Involver/Views/Shared/Components/{Name}/{Name}ViewComponent.cs` | 查資料、判斷權限、建立 ViewModel |
| `Involver/Views/Shared/Components/{Name}/Default.cshtml` | Vue 掛載點與 HTML 模板，用 `data-*` 帶入初始資料，最後引用 JS |
| `Involver/wwwroot/js/components/{name}.js` | Vue app：互動與 API 呼叫 |
| `Involver/Controllers/{Name}ApiController.cs`（需要時） | REST API，路由 `api/v1/<複數資源>` |

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
        var data = await context.Things
            .Where(t => t.ContentId == contentId)
            .AsNoTracking()
            .ToListAsync(HttpContext.RequestAborted);
        return View(new FooViewModel { ContentId = contentId, /* … */ });
    }
}
```

- ViewModel 放在同一個資料夾，或放在 `Involver/Models/ViewModels/`。每個屬性都要有 XML 註解。
- 唯讀查詢用 `AsNoTracking()`，並傳入 `CancellationToken`。

## Razor：Default.cshtml

```cshtml
@model Involver.Views.Shared.Components.Foo.FooViewModel

<div id="foo-app" data-content-id="@Model.ContentId" data-can-edit="@Model.CanEdit.ToString().ToLower()">
    <button type="button" class="btn btn-primary" @@click="saveAsync" :disabled="isSaving">儲存</button>
    <p v-if="error" class="text-danger">{{ error }}</p>
</div>

<script src="/js/components/foo.js" type="module"></script>
```

- Vue 的事件在 Razor 裡要寫成 **`@@click`**、`@@submit.prevent`。`:prop` 和 `v-bind` 可以直接寫。
- 一個頁面放多個實例時，id 要加上識別碼（`foo-app-@Model.ContentId`），JS 再用 `document.querySelectorAll('[data-foo-app]')` 逐一掛載。
- 使用者輸入的 HTML 一律在伺服器端用 `CustomHtmlSanitizer.SanitizeHtml` 消毒。在 Vue 裡用 `v-html` 顯示的內容必須是消毒過的。

## JS：foo.js

```javascript
import { createApp } from 'vue';

const app = createApp({
  data() {
    return { contentId: null, canEdit: false, isSaving: false, error: null };
  },
  watch: {},
  computed: {},
  mounted() {
    const el = document.getElementById('foo-app');
    this.contentId = Number.parseInt(el.dataset.contentId, 10);
    this.canEdit = el.dataset.canEdit === 'true';
  },
  methods: {
    /**
     * 儲存變更
     * @returns {Promise<void>}
     */
    async saveAsync() {
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const response = await fetch(`/api/v1/foos/${this.contentId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
        body: JSON.stringify({ /* … */ }),
      });
      // …
    },
  },
});

app.mount('#foo-app');
```

- 一定要 `import { createApp } from 'vue'`，不能用 `window.Vue`。
- 屬性順序固定：`data` → `watch` → `computed` → `mounted` → `methods`。
- 呼叫 API 的方法以 `Async` 結尾，命名用 get／create／update／delete 開頭。
- 程式碼風格：`const`／`let`、箭頭函式、單引號、分號、`===`。
- 有 `[ValidateAntiForgeryToken]` 的 API 要帶 `RequestVerificationToken` header。頁面上的 `<form method="post">` 會產生 token；頁面沒有表單時，要加 `@Html.AntiForgeryToken()`。

## 新的第三方 ES module

1. 用 LibMan 加進 `Involver/libman.json`（見 `dependency-upgrade` skill）。
2. 在 `Involver/Helpers/ImportMapHelper.cs` 加入對應的 specifier。
3. JS 裡用 bare specifier 匯入。

## 驗證

- 跑 `Tools/FrontendLint`：會檢查 `var`、雙引號、`window.Vue`，以及 import 有沒有在 import map 裡。
- 依 `ui-change` 的驗證步驟，在瀏覽器確認互動、淺色／深色、375 px。並在瀏覽器 console 確認沒有錯誤（Vue 模板編譯錯誤會出現在 console）。
- 有 API 時，替 controller 補上單元測試（`InvolverTest/Controllers/`）。
