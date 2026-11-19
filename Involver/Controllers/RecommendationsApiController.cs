using System.ComponentModel.DataAnnotations;

using DataAccess.Data;
using DataAccess.Models.Recommendation;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers;

/// <summary>
/// 會員首頁 Feed 的回饋：不感興趣、曝光、點擊。只有登入的會員可以呼叫
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
[Route("api/v1/recommendations")]
[ApiController]
[ValidateAntiForgeryToken]
public class RecommendationsApiController(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager) : ControllerBase
{
    /// <summary>
    /// 一次最多接受幾筆曝光
    /// </summary>
    public const int MaxImpressionsPerRequest = 60;

    /// <summary>
    /// 一張卡片
    /// </summary>
    public class ContentRequest
    {
        /// <summary>內容類型</summary>
        [Required]
        [EnumDataType(typeof(RecommendationContentType))]
        public RecommendationContentType? ContentType { get; set; }

        /// <summary>作品或文章 ID</summary>
        [Range(1, int.MaxValue)]
        public int ContentId { get; set; }
    }

    /// <summary>
    /// 一次曝光或點擊
    /// </summary>
    public class ImpressionItem : ContentRequest
    {
        /// <summary>所在區塊</summary>
        [Required]
        [EnumDataType(typeof(FeedSection))]
        public FeedSection? Section { get; set; }

        /// <summary>在區塊內的位置</summary>
        [Range(0, 1000)]
        public int Position { get; set; }

        /// <summary>產生推薦時的設定版本</summary>
        public int? SettingVersion { get; set; }
    }

    /// <summary>
    /// 一批曝光
    /// </summary>
    public class ImpressionsRequest
    {
        /// <summary>這次 Feed 頁面載入的識別碼</summary>
        [Required]
        public Guid? FeedRequestId { get; set; }

        /// <summary>進入畫面的卡片</summary>
        [Required]
        [MaxLength(MaxImpressionsPerRequest)]
        public List<ImpressionItem> Items { get; set; } = [];
    }

    /// <summary>
    /// 一次點擊
    /// </summary>
    public class ClickRequest : ImpressionItem
    {
        /// <summary>這次 Feed 頁面載入的識別碼</summary>
        [Required]
        public Guid? FeedRequestId { get; set; }
    }

    /// <summary>
    /// 不感興趣：記錄負面訊號，並立即從這位會員的推薦中移除
    /// </summary>
    /// <param name="request">內容</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204</returns>
    [HttpPost("feedback")]
    public async Task<IActionResult> CreateFeedbackAsync([FromBody] ContentRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var type = request.ContentType!.Value;
        var exists = await context.RecommendationFeedbacks
            .AnyAsync(f => f.ProfileId == userId && f.ContentType == type && f.ContentId == request.ContentId, cancellationToken);

        if (!exists)
        {
            context.RecommendationFeedbacks.Add(new RecommendationFeedback
            {
                ProfileId = userId,
                ContentType = type,
                ContentId = request.ContentId,
                CreatedAt = DateTime.UtcNow,
            });

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // 連按兩次：唯一索引擋下第二筆，結果相同
            }
        }

        await context.UserRecommendations
            .Where(r => r.ProfileId == userId && r.ContentType == type && r.ContentId == request.ContentId)
            .ExecuteDeleteAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 復原「不感興趣」。下一次排程計算後才會再出現在推薦裡
    /// </summary>
    /// <param name="contentType">內容類型</param>
    /// <param name="contentId">作品或文章 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204</returns>
    [HttpDelete("feedback/{contentType}/{contentId:int}")]
    public async Task<IActionResult> DeleteFeedbackAsync(RecommendationContentType contentType, int contentId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        await context.RecommendationFeedbacks
            .Where(f => f.ProfileId == userId && f.ContentType == contentType && f.ContentId == contentId)
            .ExecuteDeleteAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 記錄卡片曝光。同一次頁面載入、同一張卡片只記一筆
    /// </summary>
    /// <param name="request">曝光</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204</returns>
    [HttpPost("impressions")]
    public async Task<IActionResult> CreateImpressionsAsync([FromBody] ImpressionsRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var requestId = request.FeedRequestId!.Value;
        var existing = (await context.RecommendationImpressions
                .Where(i => i.FeedRequestId == requestId)
                .Select(i => new { i.ContentType, i.ContentId, i.Section })
                .ToListAsync(cancellationToken))
            .Select(i => (i.ContentType, i.ContentId, i.Section))
            .ToHashSet();

        var now = DateTime.UtcNow;
        foreach (var item in request.Items)
        {
            var key = (item.ContentType!.Value, item.ContentId, item.Section!.Value);
            if (!existing.Add(key))
            {
                continue;
            }

            context.RecommendationImpressions.Add(new RecommendationImpression
            {
                ProfileId = userId,
                FeedRequestId = requestId,
                ContentType = key.Item1,
                ContentId = key.Item2,
                Section = key.Item3,
                Position = item.Position,
                SettingVersion = key.Item3 == FeedSection.Recommended ? item.SettingVersion : null,
                ShownAt = now,
            });
        }

        await SaveIgnoringDuplicatesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 記錄卡片點擊。曝光還沒送出就點擊時，直接建立一筆已點擊的曝光
    /// </summary>
    /// <param name="request">點擊</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204</returns>
    [HttpPost("clicks")]
    public async Task<IActionResult> CreateClickAsync([FromBody] ClickRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var requestId = request.FeedRequestId!.Value;
        var type = request.ContentType!.Value;
        var section = request.Section!.Value;
        var now = DateTime.UtcNow;

        var updated = await context.RecommendationImpressions
            .Where(i => i.FeedRequestId == requestId
                && i.ProfileId == userId
                && i.ContentType == type
                && i.ContentId == request.ContentId
                && i.Section == section
                && i.ClickedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.ClickedAt, now), cancellationToken);

        if (updated == 0)
        {
            var alreadyClicked = await context.RecommendationImpressions
                .AnyAsync(i => i.FeedRequestId == requestId && i.ContentType == type && i.ContentId == request.ContentId && i.Section == section, cancellationToken);
            if (!alreadyClicked)
            {
                context.RecommendationImpressions.Add(new RecommendationImpression
                {
                    ProfileId = userId,
                    FeedRequestId = requestId,
                    ContentType = type,
                    ContentId = request.ContentId,
                    Section = section,
                    Position = request.Position,
                    SettingVersion = section == FeedSection.Recommended ? request.SettingVersion : null,
                    ShownAt = now,
                    ClickedAt = now,
                });
                await SaveIgnoringDuplicatesAsync(cancellationToken);
            }
        }

        return NoContent();
    }

    /// <summary>
    /// 曝光與點擊可能同時送達，唯一索引擋下的重複資料直接忽略
    /// </summary>
    private async Task SaveIgnoringDuplicatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
        }
    }
}
