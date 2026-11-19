using System.ComponentModel.DataAnnotations;

using DataAccess.Common;

namespace DataAccess.Models
{
    /// <summary>
    /// 標籤別名。作者輸入別名時，標籤服務會自動換成對應的正式標籤。
    /// 合併標籤時，被合併的標籤名稱會記成目標標籤的別名。
    /// </summary>
    public class TagAlias
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int TagAliasId { get; set; }

        /// <summary>
        /// 別名（顯示用）
        /// </summary>
        [Required]
        [MaxLength(Parameters.TagNameMaxLength)]
        [Display(Name = "別名")]
        public required string Name { get; set; }

        /// <summary>
        /// 統一格式後的別名（Unique），規則與 <see cref="Tag.NormalizedName"/> 相同
        /// </summary>
        [Required]
        [MaxLength(Parameters.TagNormalizedNameMaxLength)]
        public required string NormalizedName { get; set; }

        /// <summary>
        /// FK to 正式標籤
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// Reference navigation to 正式標籤
        /// </summary>
        public Tag? Tag { get; set; }

        /// <summary>
        /// 建立時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
