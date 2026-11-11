namespace DataAccess.Models.StatisticalData
{
    /// <summary>
    /// In 幣幣別。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum CoinType : byte
    {
        /// <summary>實體 In 幣（Profile.RealCoins）</summary>
        Real = 1,

        /// <summary>虛擬 In 幣（Profile.VirtualCoins）</summary>
        Virtual = 2,

        /// <summary>創作者當月收入（Profile.MonthlyCoins）</summary>
        CreatorIncome = 3,
    }

    /// <summary>
    /// In 幣交易類型。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum CoinTransactionType : short
    {
        /// <summary>儲值（任何金流商，金流商記在關聯 ID）</summary>
        TopUp = 1,

        /// <summary>投票</summary>
        Vote = 10,

        /// <summary>直接贊助創作</summary>
        InvolveNovel = 11,

        /// <summary>直接贊助文章</summary>
        InvolveArticle = 12,

        /// <summary>直接贊助創作者</summary>
        InvolveCreator = 13,

        /// <summary>每月贊助創作</summary>
        MonthlyInvolveNovel = 14,

        /// <summary>每月贊助創作者</summary>
        MonthlyInvolveCreator = 15,

        /// <summary>創作者分潤收入</summary>
        RevenueShare = 20,

        /// <summary>註冊獎勵</summary>
        SignUpBonus = 30,

        /// <summary>每日登入獎勵</summary>
        LoginReward = 31,

        /// <summary>每週任務獎勵</summary>
        MissionReward = 32,

        /// <summary>成就獎勵</summary>
        AchievementReward = 33,

        /// <summary>意見回饋被採納的獎勵</summary>
        FeedbackReward = 34,

        /// <summary>創作者收入月結歸零</summary>
        MonthlySettlement = 40,

        /// <summary>退款</summary>
        Refund = 90,

        /// <summary>管理員調整或沖正</summary>
        AdminAdjust = 99,
    }

    /// <summary>
    /// 交易關聯對象的種類。數值會存進資料庫，只能新增，不要修改既有的數值
    /// </summary>
    public enum CoinReferenceType : short
    {
        /// <summary>創作</summary>
        Novel = 1,

        /// <summary>文章（含意見回饋）</summary>
        Article = 2,

        /// <summary>投票</summary>
        Voting = 3,

        /// <summary>成就</summary>
        Achievement = 4,

        /// <summary>金流付款，關聯 ID 格式為「金流商:付款編號」</summary>
        Payment = 5,
    }
}
