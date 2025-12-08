using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using Involver.Models.ViewModels;

namespace Involver.Views.Shared.Components.Breadcrumb;

public class BreadcrumbViewComponent(ApplicationDbContext context) : ViewComponent
{
    private readonly Dictionary<string, string> _pathTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Novels", "創作" },
        { "Articles", "討論" },
        { "Episodes", "章節" },
        { "Announcements", "公告" },
        { "Feedbacks", "回報與意見" },
        { "Functions", "工具" },
        { "CreateCover", "製作小說封面" },
        { "Descriptions", "關於" },
        { "AboutInvolver", "關於 Involver" },
        { "Privacy", "隱私政策" },
        { "Terms", "服務條款" },
        { "Content", "內容政策" },
        { "Explanation", "說明" },
        { "Index", "列表" },
        { "Create", "新增" },
        { "Edit", "編輯" },
        { "Details", "詳細" },
        { "Feed", "動態" },
        { "FollowArticles", "追蹤文章" },
        { "TrendingCreations", "熱門創作" },
        { "Identity", "身分" },
        { "Profile", "個人資料" },
        { "Creations", "創作" },
        { "Interaction", "互動" },
        { "Missions", "任務" },
        { "Achievements", "成就" },
        { "Vieweds", "觀看紀錄" },
        { "Follow", "追蹤" },
        { "Messages", "留言" },
        { "Agrees", "贊同" },
        { "Notifications", "通知" },
        { "FollowCreators", "創作者" },
        { "Followers", "追蹤者" },
        { "ViewedArticles", "文章" },
        { "Involving", "Involving" },
        { "Involvers", "Involvers" },
        { "Account", "帳號" },
        { "Manage", "管理" },
        { "Email", "Email" },
        { "ChangePassword", "變更密碼" },
        { "ExternalLogins", "外部登入" },
        { "PersonalData", "個人資料" }
    };

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var breadcrumbs = new List<BreadcrumbItemViewModel>();
        var path = HttpContext.Request.Path.ToString().Trim('/').Split('/');

        breadcrumbs.Add(new BreadcrumbItemViewModel { Text = "首頁", Url = "/" });

        if (path.Length == 1 && string.IsNullOrEmpty(path[0]))
        {
            breadcrumbs.First().IsActive = true;
            breadcrumbs.First().Url = null;
            return View(breadcrumbs);
        }

        string currentUrl = "/";

        // Special handling for Identity Profile pages
        if (path.Length > 1 && path[0].Equals("Identity", StringComparison.OrdinalIgnoreCase) && path[1].Equals("Profile", StringComparison.OrdinalIgnoreCase))
        {
            string userId = HttpContext.Request.Query["id"];
            if (!string.IsNullOrEmpty(userId))
            {
                // Add "個人資料" breadcrumb linking to the user's profile page
                breadcrumbs.Add(new BreadcrumbItemViewModel { Text = _pathTranslations["Profile"], Url = $"/Identity/Profile?id={userId}" });

                // Process remaining segments like "Achievements"
                for (int i = 2; i < path.Length; i++)
                {
                    var segment = path[i];
                    if (_pathTranslations.TryGetValue(segment, out var translatedText))
                    {
                        var segmentUrl = $"/Identity/Profile/{segment}?id={userId}";
                        breadcrumbs.Add(new BreadcrumbItemViewModel { Text = translatedText, Url = segmentUrl });
                    }
                }

                // Set the last breadcrumb as active
                if (breadcrumbs.Count != 0)
                {
                    breadcrumbs.Last().IsActive = true;
                    breadcrumbs.Last().Url = null;
                }

                return View(breadcrumbs);
            }
        }

        // Special handling for Identity Account pages
        if (path.Length > 1 && path[0].Equals("Identity", StringComparison.OrdinalIgnoreCase) && path[1].Equals("Account", StringComparison.OrdinalIgnoreCase))
        {
            // Process remaining segments like "Achievements"
            for (int i = 2; i < path.Length; i++)
            {
                var segment = path[i];
                if (_pathTranslations.TryGetValue(segment, out var translatedText))
                {
                    var segmentUrl = $"/Identity/Account/{segment}";
                    breadcrumbs.Add(new BreadcrumbItemViewModel { Text = translatedText, Url = segmentUrl });
                }
            }

            // Set the last breadcrumb as active
            if (breadcrumbs.Count != 0)
            {
                breadcrumbs.Last().IsActive = true;
                breadcrumbs.Last().Url = null;
            }

            return View(breadcrumbs);
        }

        for (int i = 0; i < path.Length; i++)
        {
            var segment = path[i];
            if (string.IsNullOrEmpty(segment)) continue;

            // Special handling for Episode pages to show Novel path
            if (segment.Equals("Episodes", StringComparison.OrdinalIgnoreCase) 
                && i + 1 < path.Length && path[i+1].Equals("Details", StringComparison.OrdinalIgnoreCase))
            {
                string idFromQuery = HttpContext.Request.Query["id"];
                if (int.TryParse(idFromQuery, out int episodeId))
                {
                    var episode = await context.Episodes.AsNoTracking()
                        .Include(e => e.Novel)
                        .FirstOrDefaultAsync(e => e.EpisodeID == episodeId);

                    if (episode != null && episode.Novel != null)
                    {
                        // 1. Add "創作"
                        breadcrumbs.Add(new BreadcrumbItemViewModel { Text = _pathTranslations["Novels"], Url = "/Novels/" });
                        // 2. Add Novel Title (linking to Novel Details)
                        breadcrumbs.Add(new BreadcrumbItemViewModel { Text = episode.Novel.Title, Url = $"/Novels/Details/{episode.NovelID}" });
                        // 3. Add Episode Title (active)
                        breadcrumbs.Add(new BreadcrumbItemViewModel { Text = episode.Title, Url = null, IsActive = true });
                        i += 2; // Skip 'Episodes' and 'Details' segments
                        continue;
                    }
                }
            }

            string text = _pathTranslations.TryGetValue(segment, out var translated) ? translated : segment;
            string url = currentUrl + segment + "/";

            var action = (i + 1 < path.Length) ? path[i + 1] : null;
            bool isDetailsPage = action?.Equals("Details", StringComparison.OrdinalIgnoreCase) ?? false;

            if (isDetailsPage)
            {
                string title = null;
                // Case 1: ID in path (e.g., /Novels/Details/10)
                if (segment.Equals("Novels", StringComparison.OrdinalIgnoreCase) && i + 2 < path.Length)
                {
                    var id = path[i + 2];
                    if (int.TryParse(id, out int novelId))
                    {
                        var novel = await context.Novels.AsNoTracking().FirstOrDefaultAsync(n => n.NovelID == novelId);
                        title = novel?.Title;
                        if (title != null) i += 2; // Skip action and id
                    }
                }
                // Case 2: ID in query string (e.g., /Articles/Details?id=56)
                else
                {
                    string idFromQuery = HttpContext.Request.Query["id"];
                    if (!string.IsNullOrEmpty(idFromQuery) && segment.Equals("Articles", StringComparison.OrdinalIgnoreCase) && int.TryParse(idFromQuery, out int articleId))
                    {
                        var article = await context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ArticleID == articleId);
                        title = article?.Title;
                        if(title != null) i += 1; // Skip action
                    }
                }

                if (title != null)
                {
                    breadcrumbs.Add(new BreadcrumbItemViewModel { Text = text, Url = url });
                    breadcrumbs.Add(new BreadcrumbItemViewModel { Text = title, Url = null, IsActive = true });
                    continue;
                }
            }

            breadcrumbs.Add(new BreadcrumbItemViewModel { Text = text, Url = url });
            currentUrl = url;
        }

        if (breadcrumbs.Count != 0 && !breadcrumbs.Last().IsActive)
        {
            breadcrumbs.Last().IsActive = true;
            breadcrumbs.Last().Url = null;
        }

        return View(breadcrumbs);
    }
}
