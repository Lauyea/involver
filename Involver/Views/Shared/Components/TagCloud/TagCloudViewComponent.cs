using DataAccess.Common;
using DataAccess.Data;

using Involver.Models.ViewModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.TagCloud
{
    /// <summary>
    /// 標籤雲。只列正式標籤，使用次數小說、文章分開算，並排除軟刪除的作品。
    /// </summary>
    public class TagCloudViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public TagCloudViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 產生標籤雲
        /// </summary>
        /// <param name="tagType">Novel 或 Article</param>
        public async Task<IViewComponentResult> InvokeAsync(string tagType)
        {
            List<TagCloudItemViewModel> tags;

            string targetPage = ""; // 標籤連結的目標頁面

            var approvedTags = _context.Tags
                .Where(t => t.Status == TagStatus.Approved);

            if (tagType == "Article")
            {
                targetPage = "/Articles/Index";

                tags = await approvedTags
                    .Select(tag => new TagCloudItemViewModel
                    {
                        TagName = tag.Name,
                        Count = tag.Articles!.Count(a => !a.IsDeleted)
                    })
                    .Where(t => t.Count > 0)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else if (tagType == "Novel")
            {
                targetPage = "/Novels/Index";

                tags = await approvedTags
                    .Select(tag => new TagCloudItemViewModel
                    {
                        TagName = tag.Name,
                        Count = tag.Novels!.Count(n => !n.IsDeleted)
                    })
                    .Where(t => t.Count > 0)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                tags = new List<TagCloudItemViewModel>();
            }

            // 依使用次數決定字體大小
            if (tags.Count != 0)
            {
                double maxCount = tags.Max(t => t.Count);

                foreach (var tag in tags)
                {
                    // 分成 5 個等級
                    tag.Size = (int)Math.Ceiling(tag.Count / maxCount * 5);
                    if(tag.Size == 0)
                    {
                        tag.Size = 1;
                    }
                }
            }

            // 隨機排序，讓視覺更自然
            var random = new Random();
            var model = tags.OrderBy(t => random.Next());

            ViewBag.TargetPage = targetPage; // 將路徑傳給 View

            return View(model);
        }
    }
}
