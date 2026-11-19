using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Novels;

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

    public string SearchType { get; set; }
    public string CurrentType { get; set; }
    public string IncoinSort { get; set; }
    public string DateSort { get; set; }
    public string ViewSort { get; set; }
    public string CurrentFilter { get; set; }
    public string CurrentSort { get; set; }
    public PaginatedList<Novel> Novels { get; set; }

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

        IncoinSort = sortOrder == "Incoin_desc" ? "Incoin" : "Incoin_desc";
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

        IQueryable<Novel> NovelsIQ = from n in Context.Novels.Include("Profile").Include(n => n.Tags)
                                     select n;

        if (!String.IsNullOrEmpty(searchString))
        {
            // 標籤搜尋只找正式標籤，比對統一格式後的名稱
            var normalizedSearch = TagNameNormalizer.Normalize(searchString);
            var tagId = await Context.Tags
                .Where(t => t.NormalizedName == normalizedSearch && t.Status == TagStatus.Approved)
                .Select(t => (int?)t.TagId)
                .FirstOrDefaultAsync();

            NovelsIQ = NovelsIQ.Where(n => n.Profile.UserName.Contains(searchString)
                                   || n.Title.Contains(searchString)
                                   || (tagId != null && n.Tags.Any(t => t.TagId == tagId)));
        }

        // 分類篩選：searchType、currentType 是分類標籤 ID
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Novel, cancellationToken);
        if (int.TryParse(searchType, out var categoryId) && Categories.Any(c => c.TagId == categoryId))
        {
            NovelsIQ = NovelsIQ.Where(n => n.Tags.Any(t => t.TagId == categoryId));
        }
        else
        {
            CurrentType = null;
        }

        switch (sortOrder)
        {
            case "Date":
                NovelsIQ = NovelsIQ.OrderBy(s => s.UpdateTime);
                break;
            case "Incoin_desc":
                // 先比月收入，同分再比總收入（原本連續兩個 OrderBy，第一個排序會被蓋掉）
                NovelsIQ = NovelsIQ.OrderByDescending(s => s.MonthlyCoins).ThenByDescending(s => s.TotalCoins);
                break;
            case "Incoin":
                NovelsIQ = NovelsIQ.OrderBy(s => s.MonthlyCoins).ThenBy(s => s.TotalCoins);
                break;
            case "View_desc":
                NovelsIQ = NovelsIQ.OrderByDescending(s => s.TotalViews);
                break;
            case "View":
                NovelsIQ = NovelsIQ.OrderBy(s => s.TotalViews);
                break;
            default:
                NovelsIQ = NovelsIQ.OrderByDescending(s => s.UpdateTime);
                break;
        }

        var isAuthorized = User.IsInRole(Authorization.Novel.Novels.NovelManagersRole) ||
                       User.IsInRole(Authorization.Novel.Novels.NovelAdministratorsRole);

        var currentUserId = UserManager.GetUserId(User);

        // Only approved contacts are shown UNLESS you're authorized to see them
        // or you are the owner.
        if (!isAuthorized)
        {
            NovelsIQ = NovelsIQ.Where(a => a.Block == false
                                        || a.ProfileID == currentUserId);
        }

        // 不顯示軟刪除的資料
        NovelsIQ = NovelsIQ.Where(n => n.IsDeleted == false);

        Novels = await PaginatedList<Novel>.CreateAsync(
            NovelsIQ.AsNoTracking(), PageIndex ?? 1, Parameters.ArticlePageSize);
    }
}