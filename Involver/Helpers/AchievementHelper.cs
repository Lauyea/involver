using Involver.Common;
using Involver.Data;
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
            List<Toast> toasts = new();

            var profile = await context.Profiles
                .Where(p => p.ProfileID == profileId)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync();

            if (profile.Achievements.Where(a => a.Title == AchievementNames.Editor).FirstOrDefault() == null)
            {
                Achievement achievement = await context.Achievements.Where(a => a.Title == AchievementNames.Editor).FirstOrDefaultAsync();

                profile.Achievements.Add(achievement);

                toasts = new List<Toast>()
                    {
                        new Toast()
                        {
                            Header = AchievementNames.Editor,
                            Body = "第一次編輯"
                        }
                    };

                profile.VirtualCoins += Parameters.BronzeBadgeAward;

                context.Attach(profile).State = EntityState.Modified;

                await context.SaveChangesAsync();
            }

            return toasts;
        }
    }
}
