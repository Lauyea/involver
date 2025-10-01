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

            if (tagType == "Article")
            {
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
            if (tags.Any())
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

            return View(model);
        }
    }
}
