namespace DataAccess.Common
{
    public static class Parameters
    {
        public const int EpisodePageSize = 10;

        public const int CommetPageSize = 10;

        public const int PageSize = 10;

        public const int MessagePageSize = 5;

        public const int ArticlePageSize = 9;

        public const int VotingOptionContentLength = 15;

        /// <summary>
        /// 每部小說最多的標籤數
        /// </summary>
        public const int NovelTagSize = 8;

        /// <summary>
        /// 每篇文章最多的標籤數
        /// </summary>
        public const int ArticleTagSize = 5;

        /// <summary>
        /// 每部作品最多的分類數（分類標籤另外計算，不佔 <see cref="NovelTagSize"/>、<see cref="ArticleTagSize"/>）
        /// </summary>
        public const int CategoryMaxPerWork = 3;

        /// <summary>
        /// 標籤名稱（作者輸入的寫法）最多的字數
        /// </summary>
        public const int TagNameMaxLength = 15;

        /// <summary>
        /// 標籤統一格式後的名稱最多的字數。
        /// NFKC 可能把一個字展開成多個字（例如「㍻」→「平成」），所以比 <see cref="TagNameMaxLength"/> 長。
        /// </summary>
        public const int TagNormalizedNameMaxLength = 64;

        /// <summary>
        /// 待審標籤被幾位不同作者使用後自動轉正
        /// </summary>
        public const int TagAutoApproveAuthorCount = 3;

        /// <summary>
        /// 一般作者每天最多可以新增（申請）的標籤數
        /// </summary>
        public const int TagDailyCreateLimit = 5;

        public const int ProfileIntroLength = 256;

        public const int ArticleLength = 65536;

        public const int CommentLength = 16384;

        public const int MessageLength = 1024;

        public const string Novels = "Novels";

        public const string Feedbacks = "Feedbacks";

        public const string Announcements = "Announcements";

        public const string Episodes = "Episodes";

        public const string Articles = "Articles";

        public const int SmallContentLength = 20;

        public const int ContentLength = 20;

        public const decimal BronzeBadgeAward = 10;

        public const decimal SilverBadgeAward = 30;

        public const decimal GoldBadgeAward = 100;

        public const int ViewRecordDays = -30;

        public const int AchievementCodeLength = 32;

        public const int AchievementContentLength = 128;

        public const int ArticleTitleLength = 50;

        public const int ImageUrlLength = 1024;

        public const int NovelIntroLength = 512;

        public const int UserNameLength = 50;

        public const int MissionAwardCoins = 5;

        /// <summary>
        /// 平等模式，作者得70%分潤
        /// </summary>
        public const decimal EqualityShareRatio = 0.7m;

        /// <summary>
        /// 自由模式，作者得60%分潤
        /// </summary>
        public const decimal LibertyShareRatio = 0.6m;

        /// <summary>
        /// 最小的投票限量人數
        /// </summary>
        public const int VotingNumberLimitMin = 5;

        /// <summary>
        /// 最小的投票限值
        /// </summary>
        public const int VotingCoinLimitMin = 0;

        /// <summary>
        /// Feed 曝光與點擊紀錄保留天數（每日排程刪除更早的紀錄）
        /// </summary>
        public const int RecommendationImpressionRetentionDays = 90;
    }
}