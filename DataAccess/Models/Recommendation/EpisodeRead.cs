using System.ComponentModel.DataAnnotations;

using DataAccess.Models.NovelModel;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 章節閱讀紀錄。每位會員對每個章節只有一筆。
    /// 進入章節時建立（開始閱讀），看到章節結尾時標記 <see cref="ReachedEnd"/>。
    /// 讀完率 = 讀到結尾的人數 / 開始閱讀的人數。
    /// </summary>
    public class EpisodeRead
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 讀者 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string ProfileId { get; set; }

        /// <summary>
        /// Reference navigation to 讀者
        /// </summary>
        public Profile? Profile { get; set; }

        /// <summary>
        /// 章節 ID
        /// </summary>
        public int EpisodeId { get; set; }

        /// <summary>
        /// Reference navigation to 章節
        /// </summary>
        public Episode? Episode { get; set; }

        /// <summary>
        /// 首次閱讀時間
        /// </summary>
        public DateTime FirstReadAt { get; set; }

        /// <summary>
        /// 最近一次閱讀時間
        /// </summary>
        public DateTime LastReadAt { get; set; }

        /// <summary>
        /// 是否讀到結尾
        /// </summary>
        public bool ReachedEnd { get; set; }

        /// <summary>
        /// 第一次讀到結尾的時間
        /// </summary>
        public DateTime? ReachedEndAt { get; set; }
    }
}
