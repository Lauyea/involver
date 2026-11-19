using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 每日內容快照的內容類型。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum ContentStatType : byte
    {
        /// <summary>創作（小說）</summary>
        [Display(Name = "創作")]
        Novel = 1,

        /// <summary>文章</summary>
        [Display(Name = "文章")]
        Article = 2,
    }

    /// <summary>
    /// 管理者操作的種類。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum AdminAuditAction : short
    {
        /// <summary>任命角色</summary>
        [Display(Name = "任命角色")]
        RoleAdded = 1,

        /// <summary>移除角色</summary>
        [Display(Name = "移除角色")]
        RoleRemoved = 2,

        /// <summary>封鎖用戶</summary>
        [Display(Name = "封鎖用戶")]
        UserBanned = 3,

        /// <summary>解除封鎖</summary>
        [Display(Name = "解除封鎖")]
        UserUnbanned = 4,

        /// <summary>設為特約作家</summary>
        [Display(Name = "設為特約作家")]
        ProfessionalGranted = 5,

        /// <summary>取消特約作家</summary>
        [Display(Name = "取消特約作家")]
        ProfessionalRevoked = 6,

        /// <summary>作廢訂單</summary>
        [Display(Name = "作廢訂單")]
        PaymentVoided = 20,

        /// <summary>In 幣調整或沖正</summary>
        [Display(Name = "In 幣調整／沖正")]
        CoinAdjusted = 21,

        /// <summary>標記已撥款</summary>
        [Display(Name = "標記已撥款")]
        PayoutMarkedPaid = 22,

        /// <summary>新增網站支出</summary>
        [Display(Name = "新增網站支出")]
        SiteExpenseAdded = 23,

        /// <summary>刪除網站支出</summary>
        [Display(Name = "刪除網站支出")]
        SiteExpenseDeleted = 24,

        /// <summary>核准標籤</summary>
        [Display(Name = "核准標籤")]
        TagApproved = 40,

        /// <summary>駁回標籤</summary>
        [Display(Name = "駁回標籤")]
        TagRejected = 41,

        /// <summary>合併標籤</summary>
        [Display(Name = "合併標籤")]
        TagMerged = 42,

        /// <summary>新增標籤別名</summary>
        [Display(Name = "新增標籤別名")]
        TagAliasAdded = 43,

        /// <summary>移除標籤別名</summary>
        [Display(Name = "移除標籤別名")]
        TagAliasRemoved = 44,

        /// <summary>設定 KPI 目標</summary>
        [Display(Name = "設定 KPI 目標")]
        KpiTargetSaved = 60,

        /// <summary>刪除 KPI 目標</summary>
        [Display(Name = "刪除 KPI 目標")]
        KpiTargetDeleted = 61,
    }

    /// <summary>
    /// 可以設定目標值的指標。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum KpiMetric : short
    {
        /// <summary>每週參與投票的不重複讀者數</summary>
        [Display(Name = "每週投票讀者")]
        WeeklyVoters = 1,

        /// <summary>每月付費支持（Involve）的不重複人數</summary>
        [Display(Name = "每月付費支持者")]
        MonthlyPayingSupporters = 2,

        /// <summary>每月有更新的創作者數</summary>
        [Display(Name = "每月活躍創作者")]
        MonthlyActiveCreators = 3,

        /// <summary>總用戶數</summary>
        [Display(Name = "總用戶")]
        TotalUsers = 10,

        /// <summary>期間內新註冊</summary>
        [Display(Name = "新註冊")]
        NewUsers = 11,

        /// <summary>DAU</summary>
        [Display(Name = "DAU")]
        DailyActiveUsers = 12,

        /// <summary>MAU</summary>
        [Display(Name = "MAU")]
        MonthlyActiveUsers = 13,

        /// <summary>Prime 會員人數</summary>
        [Display(Name = "Prime 會員")]
        PrimeUsers = 14,

        /// <summary>特約作家人數</summary>
        [Display(Name = "特約作家")]
        ProfessionalUsers = 15,

        /// <summary>作品數</summary>
        [Display(Name = "作品數")]
        Novels = 16,

        /// <summary>文章數</summary>
        [Display(Name = "文章數")]
        Articles = 17,
    }

    /// <summary>
    /// KPI 目標的期間。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum KpiPeriod : byte
    {
        /// <summary>月</summary>
        [Display(Name = "月")]
        Month = 1,

        /// <summary>季</summary>
        [Display(Name = "季")]
        Quarter = 2,

        /// <summary>年</summary>
        [Display(Name = "年")]
        Year = 3,
    }

    /// <summary>
    /// 網站支出類別。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum SiteExpenseCategory : byte
    {
        /// <summary>主機</summary>
        [Display(Name = "主機")]
        Hosting = 1,

        /// <summary>網域</summary>
        [Display(Name = "網域")]
        Domain = 2,

        /// <summary>金流手續費</summary>
        [Display(Name = "金流手續費")]
        PaymentFee = 3,

        /// <summary>其他</summary>
        [Display(Name = "其他")]
        Other = 99,
    }
}
