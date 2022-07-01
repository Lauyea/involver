using System;
using System.Collections.Generic;
using System.Text;
using Involver.Models;
using Involver.Models.AnnouncementModel;
using Involver.Models.ArticleModel;
using Involver.Models.FeedbackModel;
using Involver.Models.NovelModel;
using Involver.Models.StatisticalData;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Involver.Data
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
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Dice> Dices { get; set; }
        public DbSet<Achievements> Achievements { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ProfitSharing> ProfitSharings { get; set; }

        //public DbSet<Achievement> Achievements { get; set; }

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
            modelBuilder.Entity<Profile>().ToTable("Profile");

            modelBuilder.Entity<Involving>().ToTable("Involving");
            modelBuilder.Entity<Novel>().ToTable("Novel");
            modelBuilder.Entity<Episode>().ToTable("Episode");
            modelBuilder.Entity<Comment>().ToTable("Comment");
            modelBuilder.Entity<Message>().ToTable("Message");
            modelBuilder.Entity<Agree>().ToTable("Agree");
            modelBuilder.Entity<Voting>().ToTable("Voting");
            modelBuilder.Entity<NormalOption>().ToTable("NormalOption");
            modelBuilder.Entity<BiddingOption>().ToTable("BiddingOption");
            modelBuilder.Entity<Vote>().ToTable("Vote");
            modelBuilder.Entity<Follow>().ToTable("Follow");
            modelBuilder.Entity<Missions>().ToTable("Missions");
            modelBuilder.Entity<Announcement>().ToTable("Announcement");
            modelBuilder.Entity<Feedback>().ToTable("Feedback");
            modelBuilder.Entity<Article>().ToTable("Article");
            modelBuilder.Entity<Dice>().ToTable("Dice");
            modelBuilder.Entity<Achievements>().ToTable("Achievements");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<ProfitSharing>().ToTable("ProfitSharing");
            //modelBuilder.Entity<Achievement>().ToTable("Achievements");
            base.OnModelCreating(modelBuilder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
