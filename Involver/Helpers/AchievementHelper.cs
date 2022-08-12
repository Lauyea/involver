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
        /// Beta Involver
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> BeBetaInvolverAsync(ApplicationDbContext context, string profileId)
        {
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.BetaInvolver,
                    Body = "Beta時期加入的會員",
                    Award = Parameters.GoldBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 成為駐站作家
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> BeProfessionalAsync(ApplicationDbContext context, string profileId)
        {
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Professional,
                    Body = "成為駐站作家",
                    Award = Parameters.GoldBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 第一次編輯
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FirstTimeEditAsync(ApplicationDbContext context, string profileId)
        {
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Editor,
                    Body = "第一次編輯",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Autobiographer,
                    Body = "第一次填寫自我介紹",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Assemblies,
                    Body = "第一次舉辦投票",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Taxonomist,
                    Body = "第一次使用標籤",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 取得Toasts
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="list">Toast list</param>
        /// <returns></returns>
        private static async Task<List<Toast>> GetToasts(ApplicationDbContext context, string profileId, List<Toast> list)
        {
            List<Toast> toasts = new();

            Profile profile = await GetProfileWithAchievements(context, profileId);

            foreach(var item in list)
            {
                if (profile.Achievements.Where(a => a.Title == item.Header).FirstOrDefault() == null)
                {
                    Achievement achievement = await context.Achievements.Where(a => a.Title == item.Header).FirstOrDefaultAsync();

                    profile.Achievements.Add(achievement);

                    toasts.Add(new Toast
                    {
                        Header = item.Header,
                        Body = item.Body,
                        Award = item.Award
                    });

                    profile.VirtualCoins += item.Award;

                    context.Attach(profile).State = EntityState.Modified;

                    await context.SaveChangesAsync();
                }
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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Witness,
                    Body = "第一次閱讀公告",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Gilgamesh,
                    Body = "第一次閱讀創作",
                    Award = Parameters.GoldBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.LongTimeAgo,
                    Body = "第一次閱讀章節",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Reader,
                    Body = "第一次閱讀文章",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Brainstorm,
                    Body = "第一次閱讀意見與回饋",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

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
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Rubicon,
                    Body = "第一次擲骰",
                    Award = Parameters.BronzeBadgeAward
                }
            };

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 註冊時間成就
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="enrollmentDate">註冊時間</param>
        /// <returns></returns>
        public static async Task<List<Toast>> CheckGradeAsync(ApplicationDbContext context, string profileId, DateTime enrollmentDate)
        {
            List<Toast> list = new();

            var yearAgo1 = DateTime.Now.AddYears(-1);
            var yearAgo2 = DateTime.Now.AddYears(-2);
            var yearAgo3 = DateTime.Now.AddYears(-3);
            var yearAgo4 = DateTime.Now.AddYears(-4);
            var yearAgo5 = DateTime.Now.AddYears(-5);
            var yearAgo6 = DateTime.Now.AddYears(-6);
            var yearAgo7 = DateTime.Now.AddYears(-7);

            var result1 = DateTime.Compare(yearAgo1, enrollmentDate);
            var result2 = DateTime.Compare(yearAgo2, enrollmentDate);
            var result3 = DateTime.Compare(yearAgo3, enrollmentDate);
            var result4 = DateTime.Compare(yearAgo4, enrollmentDate);
            var result5 = DateTime.Compare(yearAgo5, enrollmentDate);
            var result6 = DateTime.Compare(yearAgo6, enrollmentDate);
            var result7 = DateTime.Compare(yearAgo7, enrollmentDate);

            if (result1 > 0)
            {
                list.Add(
                    new Toast 
                    {
                        Header = AchievementNames.FirstGrade,
                        Body = "註冊滿1年",
                        Award = Parameters.BronzeBadgeAward
                    });
            }

            if (result2 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SecondGrade,
                        Body = "註冊滿2年",
                        Award = Parameters.BronzeBadgeAward
                    });
            }

            if (result3 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.ThirdGrade,
                        Body = "註冊滿3年",
                        Award = Parameters.SilverBadgeAward
                    });
            }

            if (result4 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.FourthGrade,
                        Body = "註冊滿4年",
                        Award = Parameters.SilverBadgeAward
                    });
            }

            if (result5 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.FifthGrade,
                        Body = "註冊滿5年",
                        Award = Parameters.SilverBadgeAward
                    });
            }

            if (result6 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SixthGrade,
                        Body = "註冊滿6年",
                        Award = Parameters.SilverBadgeAward
                    });
            }

            if (result7 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SeventhGrade,
                        Body = "註冊滿7年",
                        Award = Parameters.GoldBadgeAward
                    });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 投票次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> VoteCountAsync(ApplicationDbContext context, string profileId)
        {
            int voteCount = await context.Votes.Where(v => v.OwnerID == profileId).CountAsync();

            List<Toast> list = new();

            if (voteCount > 0)
            {
                list.Add(new Toast
                    {
                        Header = AchievementNames.Vote1,
                        Body = "投票1次",
                        Award = Parameters.BronzeBadgeAward
                    });
            }

            if (voteCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote30,
                    Body = "投票30次",
                    Award = Parameters.SilverBadgeAward
                });
            }

            if (voteCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote300,
                    Body = "投票300次",
                    Award = Parameters.GoldBadgeAward
                });
            }

            if (voteCount > 599)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote600,
                    Body = "投票600次",
                    Award = Parameters.GoldBadgeAward
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }
    }
}
