using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 管理者操作紀錄。只新增、不修改、不刪除。
    /// 操作者與對象都不設 FK，帳號異動後紀錄仍保留；操作者名稱另存一份當下的值。
    /// </summary>
    public class AdminAuditLog
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 操作者 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string ActorProfileId { get; set; }

        /// <summary>
        /// 操作者當下的用戶名
        /// </summary>
        [StringLength(256)]
        public string? ActorUserName { get; set; }

        /// <summary>
        /// 動作
        /// </summary>
        public AdminAuditAction Action { get; set; }

        /// <summary>
        /// 對象類型，例如 User、Payment、Tag
        /// </summary>
        [Required]
        [StringLength(32)]
        public required string TargetType { get; set; }

        /// <summary>
        /// 對象 ID
        /// </summary>
        [StringLength(450)]
        public string? TargetId { get; set; }

        /// <summary>
        /// 變更前摘要
        /// </summary>
        [StringLength(1000)]
        public string? Before { get; set; }

        /// <summary>
        /// 變更後摘要
        /// </summary>
        [StringLength(1000)]
        public string? After { get; set; }

        /// <summary>
        /// 操作時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 操作者 IP
        /// </summary>
        [StringLength(64)]
        public string? IpAddress { get; set; }
    }
}
