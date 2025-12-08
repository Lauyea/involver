using DataAccess.Data;

using Involver.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.TagCloud
{
    public class TagCloudViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public TagCloudViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string tagType)
        {
            List<TagCloudItemViewModel> tags;

            string targetPage = ""; // 宣告目標頁面變數

            if (tagType == "Article")
            {
                targetPage = "/Articles/Index"; // 如果是 Article，設定路徑

                tags = await _context.ArticleTags
                    .Select(tag => new TagCloudItemViewModel
                    {
                        TagName = tag.Name,
                        Count = tag.Articles!.Count()
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            else if (tagType == "Novel")
            {
                targetPage = "/Novels/Index"; // 如果是 Novel，設定路徑

                tags = await _context.NovelTags
                    .Select(tag => new TagCloudItemViewModel
                    {
                        TagName = tag.Name,
                        Count = tag.Novels!.Count()
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                tags = new List<TagCloudItemViewModel>();
            }

            // 計算權重，用於決定字體大小
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

            // 隨機排序，讓視覺效果更好
            var random = new Random();
            var model = tags.OrderBy(t => random.Next());

            ViewBag.TargetPage = targetPage; // 將路徑存入 ViewBag

            return View(model);
        }
    }
}
