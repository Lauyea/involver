using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Common;
using Involver.Extensions;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class ViewedArticlesModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public class ViewedArticleViewModel
    {
        public int ArticleID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime ViewDate { get; set; }
    }

    /// <summary>
    /// 摘要只需要內文開頭幾十個字；取一段夠長的前綴即可，避免載入整篇全文（可能含 base64 圖片）
    /// </summary>
    private const int ContentPrefixLength = 4000;

    public DataAccess.Models.Profile Profile { get; set; }
    public List<ViewedArticleViewModel> Articles { get; set; }

    private async Task<List<ViewedArticleViewModel>> GetViewedArticles(string id, int pageIndex = 1)
    {
        var userId = UserManager.GetUserId(User);
        if (id != userId)
        {
            return new List<ViewedArticleViewModel>(); // Or handle as an error
        }

        // 在資料庫端排序、分頁，且只取內文開頭一段做摘要，不載入全文
        var rows = await Context.Set<ArticleViewer>()
            .Where(av => av.ProfileID == id)
            .OrderByDescending(av => av.ViewDate)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .Select(av => new
            {
                av.ArticleID,
                av.Article.Title,
                ContentPrefix = av.Article.Content.Substring(0, ContentPrefixLength),
                av.ViewDate,
            })
            .AsNoTracking()
            .ToListAsync();

        var articles = rows
            .Select(r =>
            {
                return new ViewedArticleViewModel
                {
                    ArticleID = r.ArticleID,
                    Title = r.Title.Length < Parameters.SmallContentLength
                        ? r.Title
                        : r.Title.Substring(0, Parameters.SmallContentLength) + "...",
                    Content = TrimUnclosedTag(r.ContentPrefix).ToPlainTextPreview(Parameters.ContentLength),
                    ViewDate = r.ViewDate,
                };
            })
            .ToList();

        return articles;
    }

    /// <summary>
    /// 內文前綴可能在標籤中間被截斷，去掉結尾未閉合的標籤，避免被當成文字顯示
    /// </summary>
    /// <param name="prefix">內文前綴</param>
    /// <returns>結尾沒有殘缺標籤的 HTML</returns>
    private static string TrimUnclosedTag(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return string.Empty;
        }

        var lastOpen = prefix.LastIndexOf('<');

        if (lastOpen >= 0 && prefix.IndexOf('>', lastOpen) < 0)
        {
            return prefix[..lastOpen];
        }

        return prefix;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        var userId = UserManager.GetUserId(User);
        if (id != userId)
        {
            return Forbid();
        }

        Articles = await GetViewedArticles(id);

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        // 觀看紀錄只有本人看得到，載入下一頁也要檢查
        if (id != UserManager.GetUserId(User))
        {
            return Forbid();
        }

        var articles = await GetViewedArticles(id, pageIndex);
        return Partial("_ViewedArticleListPartial", articles);
    }
}
