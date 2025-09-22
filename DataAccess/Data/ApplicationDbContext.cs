using System;
using System.Collections.Generic;
using System.Text;

using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
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

        public DbSet<NovelTag> NovelTags { get; set; }

        public DbSet<ArticleTag> ArticleTags { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<View> Views { get; set; }

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

            modelBuilder.Entity<Article>()
                .HasMany(a => a.ArticleTags)
                .WithMany(t => t.Articles)
                .UsingEntity(j => j.ToTable("ArticleTaggings"));

            modelBuilder.Entity<Novel>()
                .HasMany(a => a.NovelTags)
                .WithMany(t => t.Novels)
                .UsingEntity(j => j.ToTable("NovelTaggings"));

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

            base.OnModelCreating(modelBuilder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}