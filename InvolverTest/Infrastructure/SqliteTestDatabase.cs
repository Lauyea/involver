using System;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InvolverTest.Infrastructure
{
    /// <summary>
    /// 測試用的 SQLite 記憶體資料庫。連線開著的期間資料都在；Dispose 後資料庫消失
    /// </summary>
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        /// <summary>
        /// 建立資料庫並依模型建立資料表
        /// </summary>
        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// 建立新的 DbContext（共用同一個資料庫）
        /// </summary>
        /// <returns>DbContext</returns>
        public ApplicationDbContext CreateContext() => new TestDbContext(_options);

        /// <inheritdoc />
        public void Dispose()
        {
            _connection.Dispose();
        }

        /// <summary>
        /// 調整只有 SQL Server 支援的設定，讓模型可以在 SQLite 建立
        /// </summary>
        private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // SeqNo 在 SQL Server 由資料庫產生，測試中改由程式指定
                modelBuilder.Entity<Profile>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().HasIndex(p => p.SeqNo).IsUnique(false);
                // SQLite 沒有 getdate()
                modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }
    }
}
