using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 推薦的內容類型。小說和文章共用標籤，可以互相推薦。
    /// </summary>
    public enum RecommendationContentType : byte
    {
        /// <summary>
        /// 創作（小說）
        /// </summary>
        [Display(Name = "創作")]
        Novel = 1,

        /// <summary>
        /// 文章
        /// </summary>
        [Display(Name = "文章")]
        Article = 2,
    }

    /// <summary>
    /// 會員首頁 Feed 的區塊，用來記錄曝光位置、計算各區塊的點擊率
    /// </summary>
    public enum FeedSection : byte
    {
        /// <summary>
        /// 繼續閱讀
        /// </summary>
        [Display(Name = "繼續閱讀")]
        ContinueReading = 1,

        /// <summary>
        /// 投票進行中
        /// </summary>
        [Display(Name = "投票進行中")]
        Votings = 2,

        /// <summary>
        /// 為你推薦
        /// </summary>
        [Display(Name = "為你推薦")]
        Recommended = 3,

        /// <summary>
        /// 熱門
        /// </summary>
        [Display(Name = "熱門")]
        Trending = 4,

        /// <summary>
        /// 新作
        /// </summary>
        [Display(Name = "新作")]
        Newest = 5,
    }
}
