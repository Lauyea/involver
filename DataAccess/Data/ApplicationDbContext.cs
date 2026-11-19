using System;
using System.Collections.Generic;
using System.Text;

using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.Dashboard;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Models.StatisticalData;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<InvolverUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Involving> Involvings { get; set; }
        public DbSet<Novel> Novels { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Agree> Agrees { get; set; }
        public DbSet<Voting> Votings { get; set; }
        public DbSet<NormalOption> NormalOptions { get; set; }
        public DbSet<BiddingOption> BiddingOptions { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Missions> Missions { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Dice> Dices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ProfitSharing> ProfitSharings { get; set; }

        public DbSet<Achievement> Achievements { get; set; }

        /// <summary>
        /// 標籤（小說與文章共用）
        /// </summary>
        public DbSet<Tag> Tags { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<View> Views { get; set; }
        public DbSet<Preview> Previews { get; set; }

        /// <summary>
        /// In 幣交易紀錄（流水帳）
        /// </summary>
        public DbSet<CoinTransaction> CoinTransactions { get; set; }

        /// <summary>
        /// In 幣月結
        /// </summary>
        public DbSet<CoinMonthlyBalance> CoinMonthlyBalances { get; set; }

        /// <summary>
        /// 標籤別名
        /// </summary>
        public DbSet<TagAlias> TagAliases { get; set; }

        /// <summary>
        /// 全站每日快照
        /// </summary>
        public DbSet<DailySiteStat> DailySiteStats { get; set; }

        /// <summary>
        /// 作品、文章每日快照
        /// </summary>
        public DbSet<DailyContentStat> DailyContentStats { get; set; }

        /// <summary>
        /// 管理者操作紀錄
        /// </summary>
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

        /// <summary>
        /// KPI 目標值
        /// </summary>
        public DbSet<KpiTarget> KpiTargets { get; set; }

        /// <summary>
        /// 手動登錄的網站支出
        /// </summary>
        public DbSet<SiteExpense> SiteExpenses { get; set; }

        /// <summary>
        /// 創作者分潤撥款紀錄
        /// </summary>
        public DbSet<CreatorPayout> CreatorPayouts { get; set; }

        /// <summary>
        /// 章節閱讀紀錄
        /// </summary>
        public DbSet<EpisodeRead> EpisodeReads { get; set; }

        /// <summary>
        /// 推薦的「不感興趣」
        /// </summary>
        public DbSet<RecommendationFeedback> RecommendationFeedbacks { get; set; }

        /// <summary>
        /// Feed 卡片的曝光與點擊紀錄
        /// </summary>
        public DbSet<RecommendationImpression> RecommendationImpressions { get; set; }

        /// <summary>
        /// 預先算好的個人推薦
        /// </summary>
        public DbSet<UserRecommendation> UserRecommendations { get; set; }

        /// <summary>
        /// 推薦演算法的權重與門檻（版本化）
        /// </summary>
        public DbSet<RecommendationSetting> RecommendationSettings { get; set; }

        /// <summary>
        /// 會員喜歡的主分類（冷啟動）
        /// </summary>
        public DbSet<ProfileCategoryPreference> ProfileCategoryPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Profile>(p =>
            {
                p.HasKey(e => e.ProfileID).IsClustered(false);
            });
            modelBuilder.Entity<Profile>(p =>
            {
                p.HasIndex(e => e.SeqNo).IsUnique().IsClustered();
            });

            modelBuilder.Entity<Profile>()
            .HasMany(p => p.Achievements)
            .WithMany(a => a.Profiles)
            .UsingEntity<ProfileAchievement>(
                j => j
                    .HasOne(pa => pa.Achievement)
                    .WithMany(a => a.ProfileAchievements)
                    .HasForeignKey(pa => pa.AchievementID),
                j => j
                    .HasOne(pa => pa.Profile)
                    .WithMany(p => p.ProfileAchievements)
                    .HasForeignKey(pa => pa.ProfileID),
                j =>
                {
                    j.Property(pa => pa.AchieveDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                    j.HasKey(a => new { a.ProfileID, a.AchievementID }).IsClustered(false);
                    j.HasIndex(a => a.SeqNo).IsUnique().IsClustered();
                });

            modelBuilder.Entity<Novel>()
            .HasMany(n => n.Viewers)
            .WithMany(p => p.ViewedNovels)
            .UsingEntity<NovelViewer>(
                j => j
                    .HasOne(np => np.Profile)
                    .WithMany(p => p.NovelViewers)
                    .HasForeignKey(np => np.ProfileID)
                    .OnDelete(DeleteBehavior.NoAction),
                j => j
                    .HasOne(np => np.Novel)
                    .WithMany(n => n.NovelViewers)
                    .HasForeignKey(np => np.NovelID),
                j =>
                {
                    j.Property(np => np.ViewDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                    j.HasKey(v => new { v.ProfileID, v.NovelID }).IsClustered(false);
                    j.HasIndex(v => v.SeqNo).IsUnique().IsClustered();
                });

            modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("getdate()");

            modelBuilder.Entity<Article>()
            .HasMany(a => a.Viewers)
            .WithMany(p => p.ViewedArticles)
            .UsingEntity<ArticleViewer>(
                j => j
                    .HasOne(ap => ap.Profile)
                    .WithMany(a => a.ArticleViewers)
                    .HasForeignKey(ap => ap.ProfileID)
                    .OnDelete(DeleteBehavior.NoAction),
                j => j
                    .HasOne(ap => ap.Article)
                    .WithMany(p => p.ArticleViewers)
                    .HasForeignKey(ap => ap.ArticleID),
                j =>
                {
                    j.Property(ap => ap.ViewDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                    j.HasKey(v => new { v.ProfileID, v.ArticleID }).IsClustered(false);
                    j.HasIndex(v => v.SeqNo).IsUnique().IsClustered();
                });

            // 小說、文章共用 Tags 表，但各自保留一張關聯表，兩邊都有真正的 FK
            modelBuilder.Entity<Article>()
                .HasMany(a => a.Tags)
                .WithMany(t => t.Articles)
                .UsingEntity<Dictionary<string, object>>(
                    "ArticleTaggings",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<Article>().WithMany().HasForeignKey("ArticleID"),
                    j =>
                    {
                        j.HasKey("ArticleID", "TagId");
                        j.HasIndex("TagId");
                    });

            modelBuilder.Entity<Novel>()
                .HasMany(n => n.Tags)
                .WithMany(t => t.Novels)
                .UsingEntity<Dictionary<string, object>>(
                    "NovelTaggings",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    j => j.HasOne<Novel>().WithMany().HasForeignKey("NovelID"),
                    j =>
                    {
                        j.HasKey("NovelID", "TagId");
                        j.HasIndex("TagId");
                    });

            // 為日期排序功能加上索引
            modelBuilder.Entity<Novel>()
                .HasIndex(n => n.UpdateTime);
            modelBuilder.Entity<Agree>()
                .HasIndex(a => a.UpdateTime);
            modelBuilder.Entity<Article>()
                .HasIndex(a => a.UpdateTime);
            modelBuilder.Entity<Follow>()
                .HasIndex(f => f.UpdateTime);
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.UpdateTime);
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.UpdateTime);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedDate);

            modelBuilder.Entity<ArticleViewer>()
                .HasIndex(av => av.ViewDate);

            modelBuilder.Entity<NovelViewer>()
                .HasIndex(nv => nv.ViewDate);

            modelBuilder.Entity<Profile>()
                .HasIndex(p => p.EnrollmentDate);

            // Novel 相關 Index
            modelBuilder.Entity<Novel>()
                .HasIndex(n => n.MonthlyCoins);

            modelBuilder.Entity<Novel>()
                .HasIndex(n => n.TotalCoins);

            modelBuilder.Entity<Novel>()
                .HasIndex(n => n.TotalViews);

            // involving index
            modelBuilder.Entity<Involving>()
                .HasIndex(i => i.TotalValue);
            modelBuilder.Entity<Involving>()
                .HasIndex(i => i.MonthlyValue);
            modelBuilder.Entity<Involving>()
                .HasIndex(i => i.LastTime);

            modelBuilder.Entity<Preview>(p =>
            {
                p.HasIndex(e => e.Token).IsUnique();
                p.HasIndex(e => e.ExpiresAt);
            });

            modelBuilder.Entity<CoinTransaction>(t =>
            {
                // 最主要的查詢：某人的交易明細，依時間倒序
                t.HasIndex(e => new { e.ProfileId, e.CreatedAt })
                    .IsDescending(false, true)
                    .IncludeProperties(e => new { e.CoinType, e.TransactionType, e.Amount, e.BalanceAfter });

                // 月排程依時間範圍彙總
                t.HasIndex(e => e.CreatedAt);

                t.HasIndex(e => e.CorrelationId);

                // 金流商重送付款通知時，由資料庫擋掉重複入帳
                t.HasIndex(e => e.IdempotencyKey)
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL");

                // 帳務紀錄不能因為刪除使用者而被連帶刪除
                t.HasOne<Profile>()
                    .WithMany()
                    .HasForeignKey(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CoinMonthlyBalance>(b =>
            {
                b.HasIndex(e => new { e.ProfileId, e.CoinType, e.Month }).IsUnique();
                b.HasIndex(e => e.Month);

                b.HasOne<Profile>()
                    .WithMany()
                    .HasForeignKey(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            ConfigureIssue219(modelBuilder);
            ConfigureAccountOwnership(modelBuilder);
            ConfigureDashboard(modelBuilder);
            ConfigureRecommendation(modelBuilder);

            base.OnModelCreating(modelBuilder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        /// <summary>
        /// 產生「剛好填一個」的 CHECK 條件，例如 (CASE WHEN [A] IS NULL THEN 0 ELSE 1 END + ...) = 1
        /// </summary>
        /// <param name="columns">多種父表共用的外鍵欄位</param>
        /// <returns>SQL Server 與 SQLite 都能用的 CHECK 條件</returns>
        private static string ExactlyOneNotNull(params string[] columns)
        {
            var terms = columns.Select(c => $"CASE WHEN [{c}] IS NULL THEN 0 ELSE 1 END");
            return $"({string.Join(" + ", terms)}) = 1";
        }

        /// <summary>
        /// Issue #219：多父表外鍵的 CHECK 約束、Unique 約束與查詢用的複合索引
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        private static void ConfigureIssue219(ModelBuilder modelBuilder)
        {
            // 多種父表共用外鍵欄位：資料庫保證剛好填一個
            modelBuilder.Entity<Comment>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Comments_ExactlyOneParent",
                    ExactlyOneNotNull(nameof(Comment.NovelID), nameof(Comment.EpisodeID), nameof(Comment.ArticleID))));

            modelBuilder.Entity<Involving>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Involvings_ExactlyOneTarget",
                    ExactlyOneNotNull(nameof(Involving.ProfileID), nameof(Involving.NovelID), nameof(Involving.ArticleID))));

            modelBuilder.Entity<Follow>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Follows_ExactlyOneTarget",
                    ExactlyOneNotNull(nameof(Follow.ProfileID), nameof(Follow.NovelID))));

            modelBuilder.Entity<Agree>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Agrees_ExactlyOneTarget",
                    ExactlyOneNotNull(nameof(Agree.CommentID), nameof(Agree.MessageID))));

            modelBuilder.Entity<View>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Views_ExactlyOneTarget",
                    ExactlyOneNotNull(nameof(View.ArticleId), nameof(View.NovelId))));

            // 通知下拉選單：某人最新的通知與未讀數
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.ProfileID, n.CreatedDate })
                .IncludeProperties(n => n.IsRead);

            // 觀看紀錄：去重檢查、統計圖表；日結刪除用既有的 CreateTime 單欄索引
            modelBuilder.Entity<View>(v =>
            {
                v.HasIndex(e => new { e.NovelId, e.CreateTime });
                v.HasIndex(e => new { e.ArticleId, e.CreateTime });
                v.HasIndex(e => e.CreateTime);
            });

            // 是否已追蹤；同一人對同一對象只能有一筆
            modelBuilder.Entity<Follow>(f =>
            {
                f.HasIndex(e => new { e.ProfileID, e.FollowerID })
                    .IsUnique()
                    .HasFilter("[ProfileID] IS NOT NULL");
                f.HasIndex(e => new { e.NovelID, e.FollowerID })
                    .IsUnique()
                    .HasFilter("[NovelID] IS NOT NULL");
            });

            // 是否已按讚；同一人對同一則留言／回覆只能有一筆
            modelBuilder.Entity<Agree>(a =>
            {
                a.HasIndex(e => new { e.CommentID, e.ProfileID })
                    .IsUnique()
                    .HasFilter("[CommentID] IS NOT NULL");
                a.HasIndex(e => new { e.MessageID, e.ProfileID })
                    .IsUnique()
                    .HasFilter("[MessageID] IS NOT NULL");
            });

            // 瀏覽紀錄分頁
            modelBuilder.Entity<ArticleViewer>()
                .HasIndex(v => new { v.ProfileID, v.ViewDate });
            modelBuilder.Entity<NovelViewer>()
                .HasIndex(v => new { v.ProfileID, v.ViewDate });

            // 公告、意見回饋列表
            modelBuilder.Entity<Article>()
                .HasIndex(a => new { a.Type, a.UpdateTime });

            // 登入、註冊以用戶名查詢；資料庫層防止同名帳號
            modelBuilder.Entity<Profile>()
                .HasIndex(p => p.UserName)
                .IsUnique();

            // 標籤名稱不重複（SQL Server 預設定序不分大小寫）
            modelBuilder.Entity<Tag>(t =>
            {
                // 所有比對都用統一格式後的名稱；Name 只用來顯示
                t.HasIndex(e => e.NormalizedName).IsUnique();
                // 建議清單、標籤雲只列正式標籤
                t.HasIndex(e => e.Status);
                // 分類標籤清單依顯示順序列出
                t.HasIndex(e => new { e.IsCategory, e.CategoryOrder });
                t.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                // 帳號不會被實體刪除；不設 cascade，避免和作品的刪除路徑衝突
                t.HasOne(e => e.CreatorProfile)
                    .WithMany()
                    .HasForeignKey(e => e.CreatorProfileID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // 付款回呼在 Serializable 交易裡以 MerchantTradeNo 查詢（綠界規格最長 20 字元）
            modelBuilder.Entity<Payment>(p =>
            {
                p.Property(e => e.MerchantTradeNo).HasMaxLength(64);
                p.HasIndex(e => e.MerchantTradeNo);
                p.Property(e => e.InvolverID).HasMaxLength(450);
                p.HasIndex(e => e.InvolverID);
            });

            // Owner 欄位：成就計數等查詢依擁有者篩選
            modelBuilder.Entity<Episode>(e =>
            {
                e.Property(x => x.OwnerID).HasMaxLength(450);
                e.HasIndex(x => x.OwnerID);
            });
            modelBuilder.Entity<Voting>(v =>
            {
                v.Property(x => x.OwnerID).HasMaxLength(450);
                v.HasIndex(x => x.OwnerID);
            });
            modelBuilder.Entity<Vote>(v =>
            {
                v.Property(x => x.OwnerID).HasMaxLength(450);
                v.HasIndex(x => x.OwnerID);
            });
        }

        /// <summary>
        /// Issue #237：儀表板的每日快照、管理者操作紀錄、KPI 目標、網站支出、撥款紀錄與標籤別名
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        /// <summary>
        /// Issue #238：章節閱讀紀錄、曝光紀錄、不感興趣、預先算好的推薦與推薦設定
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        private static void ConfigureRecommendation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EpisodeRead>(r =>
            {
                // 每人每章一筆；也用來查「某人讀過哪些章節」
                r.HasIndex(e => new { e.ProfileId, e.EpisodeId }).IsUnique();
                // 讀完率依章節彙總
                r.HasIndex(e => e.EpisodeId).IncludeProperties(e => e.ReachedEnd);
                // 帳號不會被實體刪除；章節刪除時連帶刪除閱讀紀錄
                r.HasOne(e => e.Profile)
                    .WithMany()
                    .HasForeignKey(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                r.HasOne(e => e.Episode)
                    .WithMany()
                    .HasForeignKey(e => e.EpisodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecommendationFeedback>(f =>
            {
                f.HasIndex(e => new { e.ProfileId, e.ContentType, e.ContentId }).IsUnique();
            });

            modelBuilder.Entity<RecommendationImpression>(i =>
            {
                // 點擊時以 (FeedRequestId, 內容, 區塊) 找回曝光；同一次載入同一張卡片只記一筆
                i.HasIndex(e => new { e.FeedRequestId, e.ContentType, e.ContentId, e.Section }).IsUnique();
                // CTR 報表與日結刪除依時間範圍
                i.HasIndex(e => e.ShownAt).IncludeProperties(e => new { e.Section, e.SettingVersion, e.ClickedAt });
            });

            modelBuilder.Entity<UserRecommendation>(r =>
            {
                r.HasIndex(e => new { e.ProfileId, e.Rank });
            });

            modelBuilder.Entity<RecommendationSetting>(s =>
            {
                s.HasIndex(e => e.Version).IsUnique();
                // 同時只有一個版本啟用
                s.HasIndex(e => e.IsActive)
                    .IsUnique()
                    .HasFilter("[IsActive] = 1");
            });

            modelBuilder.Entity<ProfileCategoryPreference>(p =>
            {
                p.HasKey(e => new { e.ProfileId, e.TagId });
                p.HasOne(e => e.Profile)
                    .WithMany()
                    .HasForeignKey(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
                // 標籤被合併或刪除時，偏好跟著刪除
                p.HasOne(e => e.Tag)
                    .WithMany()
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureDashboard(ModelBuilder modelBuilder)
        {
            // 同一內容每天只有一筆；作品趨勢圖依內容與日期查詢
            modelBuilder.Entity<DailyContentStat>(s =>
            {
                s.HasIndex(e => new { e.ContentType, e.ContentId, e.Date }).IsUnique();
                s.HasIndex(e => new { e.OwnerProfileId, e.Date });
                s.HasIndex(e => e.Date);
            });

            modelBuilder.Entity<AdminAuditLog>(l =>
            {
                l.HasIndex(e => e.CreatedAt);
                l.HasIndex(e => new { e.ActorProfileId, e.CreatedAt });
                l.HasIndex(e => new { e.Action, e.CreatedAt });
            });

            modelBuilder.Entity<KpiTarget>()
                .HasIndex(e => new { e.Metric, e.Period, e.PeriodStart })
                .IsUnique();

            modelBuilder.Entity<SiteExpense>()
                .HasIndex(e => e.Date);

            modelBuilder.Entity<CreatorPayout>(p =>
            {
                p.HasIndex(e => new { e.ProfileId, e.Month }).IsUnique();
                p.HasIndex(e => e.PaidAt);

                // 撥款是帳務紀錄，不能因為刪除使用者而被連帶刪除
                p.HasOne<Profile>()
                    .WithMany()
                    .HasForeignKey(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 別名和正式標籤共用同一套統一格式；同一個寫法只能對應一個標籤
            modelBuilder.Entity<TagAlias>(a =>
            {
                a.HasIndex(e => e.NormalizedName).IsUnique();
                a.HasOne(e => e.Tag)
                    .WithMany(t => t.Aliases)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 訂單列表依付款時間篩選
            modelBuilder.Entity<Payment>()
                .HasIndex(e => e.PaymentTime);
        }

        /// <summary>
        /// Issue #219：Profile 與 AspNetUsers 的 1:1 關聯，以及擁有者欄位的外鍵
        /// </summary>
        /// <remarks>
        /// 帳號刪除改為匿名化，Profile 與 AspNetUsers 都不會被刪除，因此一律用 Restrict，
        /// 避免誤刪帳號時連帶刪掉作品或 In 幣流水帳。
        /// </remarks>
        /// <param name="modelBuilder">Model builder</param>
        private static void ConfigureAccountOwnership(ModelBuilder modelBuilder)
        {
            // Profile.ProfileID 就是 AspNetUsers.Id
            modelBuilder.Entity<Profile>()
                .HasOne<InvolverUser>()
                .WithOne()
                .HasForeignKey<Profile>(p => p.ProfileID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Episode>()
                .HasOne<Profile>()
                .WithMany()
                .HasForeignKey(e => e.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Voting>()
                .HasOne<Profile>()
                .WithMany()
                .HasForeignKey(v => v.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vote>()
                .HasOne<Profile>()
                .WithMany()
                .HasForeignKey(v => v.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}