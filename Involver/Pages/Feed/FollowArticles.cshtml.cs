using DataAccess.Common;
using DataAccess.Data;

using Involver.Extensions;
using Involver.Models.ViewModels.Feed;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feed
{
    /// <summary>
    /// 追蹤文章：目前登入的會員所追蹤的創作者發表的文章，依更新時間由新到舊。
    /// 舊網址的 <c>?id=</c> 參數不再使用，一律顯示自己的追蹤（避免看到別人追蹤了誰）
    /// </summary>
    /// <param name="context">資料庫內容</param>
    /// <param name="userManager">帳號管理</param>
    public class FollowArticlesModel(
        ApplicationDbContext context,
        UserManager<InvolverUser> userManager) : PageModel
    {
        /// <summary>
        /// 每頁筆數
        /// </summary>
        public const int PageSize = 20;

        /// <summary>
        /// 預覽的字數
        /// </summary>
        public const int PreviewLength = 120;

        /// <summary>
        /// 第幾頁（從 1 開始）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 總頁數；沒有文章時為 0
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// 追蹤中的創作者人數
        /// </summary>
        public int FollowedCreatorCount { get; private set; }

        /// <summary>
        /// 這一頁的文章
        /// </summary>
        public List<FollowedArticleViewModel> Articles { get; private set; } = [];

        /// <summary>
        /// 讀取追蹤的創作者的文章
        /// </summary>
        /// <param name="cancellationToken">取消權杖</param>
        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User);
            var authorIds = context.Follows
                .Where(f => f.FollowerID == userId && f.ProfileID != null)
                .Select(f => f.ProfileID!);

            FollowedCreatorCount = await authorIds
                .Distinct()
                .CountAsync(cancellationToken);
            if (FollowedCreatorCount == 0)
            {
                return;
            }

            var articles = context.Articles
                .Where(a => !a.IsDeleted && !a.Block && authorIds.Contains(a.ProfileID));

            var total = await articles.CountAsync(cancellationToken);
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
            PageIndex = Math.Clamp(PageIndex, 1, Math.Max(TotalPages, 1));

            var rows = await articles
                .OrderByDescending(a => a.UpdateTime)
                .ThenByDescending(a => a.ArticleID)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .Select(a => new
                {
                    a.ArticleID,
                    a.Title,
                    a.Content,
                    a.ImageUrl,
                    a.ProfileID,
                    AuthorName = a.Profile!.UserName,
                    AuthorImageUrl = a.Profile.ImageUrl,
                    a.UpdateTime,
                    a.TotalViews,
                    Comments = a.Comments!.Count(),
                    Tags = a.Tags!
                        .Where(t => t.Status == TagStatus.Approved)
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToList(),
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            Articles = rows
                .Select(a => new FollowedArticleViewModel
                {
                    ArticleId = a.ArticleID,
                    Title = a.Title,
                    Preview = a.Content.ToPlainTextPreview(PreviewLength),
                    ImageUrl = string.IsNullOrWhiteSpace(a.ImageUrl) ? null : a.ImageUrl,
                    AuthorId = a.ProfileID,
                    AuthorName = a.AuthorName,
                    AuthorImageUrl = string.IsNullOrWhiteSpace(a.AuthorImageUrl) ? null : a.AuthorImageUrl,
                    UpdatedAt = a.UpdateTime,
                    Views = a.TotalViews,
                    Comments = a.Comments,
                    Tags = a.Tags,
                })
                .ToList();
        }
    }
}
