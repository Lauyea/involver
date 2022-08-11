using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.AchievementModel;
using Involver.Models.ArticleModel;
using Microsoft.EntityFrameworkCore;

namespace Involver.Helpers
{
    public static class AchievementHelper
    {
        /// <summary>
        /// 第一次編輯
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FirstTimeEditAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Editor;

            string body = "第一次編輯";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次填寫自我介紹
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> BeAutobiographerAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Autobiographer;

            string body = "第一次填寫自我介紹";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次舉辦投票
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FirstTimeCreateVotingAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Assemblies;

            string body = "第一次舉辦投票";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次使用標籤
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FirstTimeUseTagsAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Taxonomist;

            string body = "第一次使用標籤";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 取得Toasts
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="header">Header string</param>
        /// <param name="body">Body string</param>
        /// <param name="award">Award value</param>
        /// <returns></returns>
        private static async Task<List<Toast>> GetToasts(ApplicationDbContext context, string profileId, string header, string body, decimal award)
        {
            List<Toast> toasts = new();

            Profile profile = await GetProfileWithAchievements(context, profileId);

            if (profile.Achievements.Where(a => a.Title == header).FirstOrDefault() == null)
            {
                Achievement achievement = await context.Achievements.Where(a => a.Title == header).FirstOrDefaultAsync();

                profile.Achievements.Add(achievement);

                toasts = new List<Toast>()
                    {
                        new Toast()
                        {
                            Header = header,
                            Body = body
                        }
                    };

                profile.VirtualCoins += award;

                context.Attach(profile).State = EntityState.Modified;

                await context.SaveChangesAsync();
            }

            return toasts;
        }

        private static async Task<Profile> GetProfileWithAchievements(ApplicationDbContext context, string profileId)
        {
            return await context.Profiles
                            .Where(p => p.ProfileID == profileId)
                            .Include(p => p.Achievements)
                            .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 第一次閱讀公告
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ReadAnnouncementAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Witness;

            string body = "第一次閱讀公告";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次閱讀創作
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ReadNovelAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Gilgamesh;

            string body = "第一次閱讀創作";

            decimal award = Parameters.GoldBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次閱讀章節
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ReadEpisodeAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.LongTimeAgo;

            string body = "第一次閱讀章節";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次閱讀文章
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ReadArticleAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Reader;

            string body = "第一次閱讀文章";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次閱讀意見與回饋
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ReadFeedbackAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Brainstorm;

            string body = "第一次閱讀意見與回饋";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }

        /// <summary>
        /// 第一次擲骰
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> RollDicesAsync(ApplicationDbContext context, string profileId)
        {
            string header = AchievementNames.Rubicon;

            string body = "第一次擲骰";

            decimal award = Parameters.BronzeBadgeAward;

            var toasts = await GetToasts(context, profileId, header, body, award);

            return toasts;
        }
    }
}
