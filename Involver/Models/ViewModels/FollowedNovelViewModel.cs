using DataAccess.Models;

namespace Involver.Models.ViewModels;

/// <summary>
/// 追蹤中創作列表的一列：只帶顯示需要的欄位，不載入章節全文
/// </summary>
public class FollowedNovelViewModel
{
    /// <summary>
    /// 小說 ID
    /// </summary>
    public int NovelID { get; set; }

    /// <summary>
    /// 小說標題
    /// </summary>
    public string NovelTitle { get; set; }

    /// <summary>
    /// 小說最後更新時間
    /// </summary>
    public DateTime NovelUpdateTime { get; set; }

    /// <summary>
    /// 作者 ProfileID
    /// </summary>
    public string AuthorID { get; set; }

    /// <summary>
    /// 作者用戶名
    /// </summary>
    public string AuthorUserName { get; set; }

    /// <summary>
    /// 是否為對創作的月贊助者
    /// </summary>
    public bool NovelMonthlyInvolver { get; set; }

    /// <summary>
    /// 最新一集的 ID（依 EpisodeID 最大者），沒有章節時為 null
    /// </summary>
    public int? LatestEpisodeID { get; set; }

    /// <summary>
    /// 最新一集的標題，沒有章節時為 null
    /// </summary>
    public string LatestEpisodeTitle { get; set; }

    /// <summary>
    /// 查詢某人追蹤的小說，依小說更新時間倒序，並帶出最新一集的 ID 與標題
    /// </summary>
    /// <param name="follows">Follows 資料來源</param>
    /// <param name="followerId">追蹤者的 ProfileID</param>
    /// <returns>可再分頁的查詢</returns>
    public static IQueryable<FollowedNovelViewModel> QueryByFollower(IQueryable<Follow> follows, string followerId)
    {
        return follows
            .Where(f => f.FollowerID == followerId)
            .Where(f => f.NovelID != null)
            .OrderByDescending(f => f.Novel.UpdateTime)
            .Select(f => new FollowedNovelViewModel
            {
                NovelID = f.Novel.NovelID,
                NovelTitle = f.Novel.Title,
                NovelUpdateTime = f.Novel.UpdateTime,
                AuthorID = f.Novel.ProfileID,
                AuthorUserName = f.Novel.Profile.UserName,
                NovelMonthlyInvolver = f.NovelMonthlyInvolver,
                LatestEpisodeID = f.Novel.Episodes
                    .OrderByDescending(e => e.EpisodeID)
                    .Select(e => (int?)e.EpisodeID)
                    .FirstOrDefault(),
                LatestEpisodeTitle = f.Novel.Episodes
                    .OrderByDescending(e => e.EpisodeID)
                    .Select(e => e.Title)
                    .FirstOrDefault(),
            });
    }
}
