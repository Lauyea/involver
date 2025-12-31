using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Common;
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
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public PaginatedList<Article> Articles { get; set; }
    public string DateSort { get; set; }
    public string ViewSort { get; set; }
    public string CurrentFilter { get; set; }
    public string CurrentSort { get; set; }

    public async Task OnGetAsync(
        string sortOrder,
        string currentFilter,
        string searchString,
        int? PageIndex)
    {
        CurrentSort = sortOrder;
        DateSort = sortOrder == "Date_desc" ? "Date" : "Date_desc";
        ViewSort = sortOrder == "View_desc" ? "View" : "View_desc";

        if (searchString != null)
        {
            PageIndex = 1;
        }
        else
        {
            searchString = currentFilter;
        }
        CurrentFilter = searchString;

        var articles = Context.Articles
            .Include(a => a.Comments)
            .Include(a => a.Profile)
            .Include(a => a.ArticleTags)
            .Where(a => a.Type == ArticleType.General)
            .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            ArticleTag articleTag = await Context.ArticleTags.Where(t => t.Name == searchString).FirstOrDefaultAsync();

            articles = articles
                .Where(a => a.Profile.UserName.Contains(searchString)
                || a.Title.Contains(searchString)
                || a.ArticleTags.Contains(articleTag));
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
    }
}