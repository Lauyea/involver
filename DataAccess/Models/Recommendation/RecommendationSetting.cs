using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 推薦演算法的權重與門檻。每次修改都新增一個版本，不覆蓋舊的；同時只有一個版本 <see cref="IsActive"/>。
    /// 正式數值只透過管理員後台輸入，不寫進 migration 或 HasData（repo 是公開的）。
    /// </summary>
    public class RecommendationSetting
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 版本號（Unique，由 1 開始遞增）
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 設定內容（JSON），格式見 <c>DataAccess.Services.Recommendations.RecommendationSettings</c>
        /// </summary>
        [Required]
        public required string SettingsJson { get; set; }

        /// <summary>
        /// 是否為目前啟用的版本
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 建立者 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string CreatedBy { get; set; }

        /// <summary>
        /// 建立時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 修改說明
        /// </summary>
        [StringLength(200)]
        public string? Note { get; set; }
    }
}
