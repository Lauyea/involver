using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles;

[AllowAnonymous]
public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ICategoryService categoryService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 分類篩選的選項
    /// </summary>
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];

    /// <summary>
    /// 目前的分類篩選（分類標籤 ID）
    /// </summary>
    public string CurrentType { get; set; }

    public PaginatedList<Article> Articles { get; set; }
    public string DateSort { get; set; }
    public string ViewSort { get; set; }
    public string CurrentFilter { get; set; }
    public string CurrentSort { get; set; }

    /// <summary>
    /// 本頁各文章的留言數（ArticleID → 留言數）
    /// </summary>
    public Dictionary<int, int> CommentCounts { get; set; } = [];

    /// <summary>
    /// 本頁各文章的純文字預覽（ArticleID → 預覽文字），已去除 HTML 標籤並截斷
    /// </summary>
    public Dictionary<int, string> Previews { get; set; } = [];

    public async Task OnGetAsync(
        string currentType,
        string searchType,
        string sortOrder,
        string currentFilter,
        string searchString,
        int? PageIndex,
        CancellationToken cancellationToken)
    {
        CurrentSort = sortOrder;
        DateSort = sortOrder == "Date_desc" ? "Date" : "Date_desc";
        ViewSort = sortOrder == "View_desc" ? "View" : "View_desc";

        if (searchType != null || searchString != null)
        {
            PageIndex = 1;
        }
        else
        {
            searchString = currentFilter;
            searchType = currentType;
        }
        CurrentFilter = searchString;
        CurrentType = searchType;

        var articles = Context.Articles
            .Include(a => a.Profile)
            .Include(a => a.Tags)
            .Where(a => a.Type == ArticleType.General)
            .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            // 標籤搜尋只找正式標籤，比對統一格式後的名稱
            var normalizedSearch = TagNameNormalizer.Normalize(searchString);
            var tagId = await Context.Tags
                .Where(t => t.NormalizedName == normalizedSearch && t.Status == TagStatus.Approved)
                .Select(t => (int?)t.TagId)
                .FirstOrDefaultAsync();

            articles = articles
                .Where(a => a.Profile.UserName.Contains(searchString)
                || a.Title.Contains(searchString)
                || (tagId != null && a.Tags.Any(t => t.TagId == tagId)));
        }

        // 分類篩選：searchType、currentType 是分類標籤 ID
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Article, cancellationToken);
        if (int.TryParse(searchType, out var categoryId) && Categories.Any(c => c.TagId == categoryId))
        {
            articles = articles.Where(a => a.Tags.Any(t => t.TagId == categoryId));
        }
        else
        {
            CurrentType = null;
        }

        switch (sortOrder)
        {
            case "Date":
                articles = articles.OrderBy(s => s.UpdateTime);
                break;
            case "View_desc":
                articles = articles.OrderByDescending(s => s.TotalViews);
                break;
            case "View":
                articles = articles.OrderBy(s => s.TotalViews);
                break;
            default:
                articles = articles.OrderByDescending(s => s.UpdateTime);
                break;
        }


        var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                       User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

        var currentUserId = UserManager.GetUserId(User);

        // Only approved contacts are shown UNLESS you're authorized to see them
        // or you are the owner.
        if (!isAuthorized)
        {
            articles = articles.Where(a => a.Block == false
                                        || a.ProfileID == currentUserId);
        }

        // 不顯示軟刪除的資料
        articles = articles.Where(a => a.IsDeleted == false);

        Articles = await PaginatedList<Article>.CreateAsync(
            articles.AsNoTracking(), PageIndex ?? 1, Parameters.ArticlePageSize);

        // 只為了顯示留言數，改在資料庫端計數，不載入整個留言集合
        var articleIds = Articles.Select(a => a.ArticleID).ToList();

        Previews = Articles.ToDictionary(
            a => a.ArticleID,
            a => a.Content.ToPlainTextPreview(Parameters.ContentLength));

        CommentCounts = await Context.Comments
            .Where(c => c.ArticleID != null && articleIds.Contains(c.ArticleID.Value))
            .GroupBy(c => c.ArticleID.Value)
            .Select(g => new { ArticleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Count);
    }
}