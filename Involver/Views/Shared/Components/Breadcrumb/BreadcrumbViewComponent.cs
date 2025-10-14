using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Data;
using Involver.Models.ViewModels;

namespace Involver.Views.Shared.Components.Breadcrumb
{
    public class BreadcrumbViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<string, string> _pathTranslations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
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
            { "Details", "詳細" }
        };

        public BreadcrumbViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

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
            for (int i = 0; i < path.Length; i++)
            {
                var segment = path[i];
                if (string.IsNullOrEmpty(segment)) continue;

                // Special handling for Episode pages to show Novel path
                if (segment.Equals("Episodes", System.StringComparison.OrdinalIgnoreCase) 
                    && i + 1 < path.Length && path[i+1].Equals("Details", System.StringComparison.OrdinalIgnoreCase))
                {
                    string idFromQuery = HttpContext.Request.Query["id"];
                    if (int.TryParse(idFromQuery, out int episodeId))
                    {
                        var episode = await _context.Episodes.AsNoTracking()
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
                bool isDetailsPage = action?.Equals("Details", System.StringComparison.OrdinalIgnoreCase) ?? false;

                if (isDetailsPage)
                {
                    string title = null;
                    // Case 1: ID in path (e.g., /Novels/Details/10)
                    if (segment.Equals("Novels", System.StringComparison.OrdinalIgnoreCase) && i + 2 < path.Length)
                    {
                        var id = path[i + 2];
                        if (int.TryParse(id, out int novelId))
                        {
                            var novel = await _context.Novels.AsNoTracking().FirstOrDefaultAsync(n => n.NovelID == novelId);
                            title = novel?.Title;
                            if (title != null) i += 2; // Skip action and id
                        }
                    }
                    // Case 2: ID in query string (e.g., /Articles/Details?id=56)
                    else
                    {
                        string idFromQuery = HttpContext.Request.Query["id"];
                        if (!string.IsNullOrEmpty(idFromQuery) && segment.Equals("Articles", System.StringComparison.OrdinalIgnoreCase) && int.TryParse(idFromQuery, out int articleId))
                        {
                            var article = await _context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ArticleID == articleId);
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

            if (breadcrumbs.Any() && !breadcrumbs.Last().IsActive)
            {
                breadcrumbs.Last().IsActive = true;
                breadcrumbs.Last().Url = null;
            }

            return View(breadcrumbs);
        }
    }
}
