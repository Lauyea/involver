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
            { "Edit", "編輯" }
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

                string text = _pathTranslations.TryGetValue(segment, out var translated) ? translated : segment;
                string url = currentUrl + segment + "/";

                // Check for dynamic segments (Details/Edit pages)
                if (i + 1 < path.Length && (segment.Equals("Novels", System.StringComparison.OrdinalIgnoreCase) || segment.Equals("Articles", System.StringComparison.OrdinalIgnoreCase)))
                {
                    var action = path[i + 1];
                    if (action.Equals("Details", System.StringComparison.OrdinalIgnoreCase) || action.Equals("Edit", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 2 < path.Length)
                        {
                            var id = path[i + 2];
                            string title = null;

                            if (segment.Equals("Novels", System.StringComparison.OrdinalIgnoreCase) && int.TryParse(id, out int novelId))
                            {
                                var novel = await _context.Novels.AsNoTracking().FirstOrDefaultAsync(n => n.NovelID == novelId);
                                title = novel?.Title;
                            }
                            else if (segment.Equals("Articles", System.StringComparison.OrdinalIgnoreCase) && int.TryParse(id, out int articleId))
                            {
                                var article = await _context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ArticleID == articleId);
                                title = article?.Title;
                            }

                            if (title != null)
                            {
                                // Add the parent (e.g., "創作")
                                breadcrumbs.Add(new BreadcrumbItemViewModel { Text = text, Url = url });
                                // Add the dynamic title
                                breadcrumbs.Add(new BreadcrumbItemViewModel { Text = title, Url = null, IsActive = true });
                                i += 2; // Skip action and id segments
                                continue;
                            }
                        }
                    }
                }

                breadcrumbs.Add(new BreadcrumbItemViewModel { Text = text, Url = url });
                currentUrl = url;
            }

            if (breadcrumbs.Any())
            {
                var last = breadcrumbs.Last();
                if (!last.IsActive)
                {
                    last.IsActive = true;
                    last.Url = null;
                }
            }

            return View(breadcrumbs);
        }
    }
}
