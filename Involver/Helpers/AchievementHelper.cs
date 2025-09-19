using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;

using Involver.Common;

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
                    Header = AchievementNames.BetaInvolver
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
                    Header = AchievementNames.Professional
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
                    Header = AchievementNames.Editor
                }
            };

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 第一次刪除
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FirstTimeDeleteAsync(ApplicationDbContext context, string profileId)
        {
            List<Toast> list = new()
            {
                new Toast
                {
                    Header = AchievementNames.Discipline
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
                    Header = AchievementNames.Autobiographer
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
                    Header = AchievementNames.Assemblies
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
                    Header = AchievementNames.Taxonomist
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

            if (profile == null)
            {
                return toasts;
            }

            foreach (var item in list)
            {
                if (profile.Achievements.Where(a => a.Code == item.Header).FirstOrDefault() == null)
                {
                    Achievement achievement = await context.Achievements.Where(a => a.Code == item.Header).FirstOrDefaultAsync();

                    if (achievement == null)
                    {
                        continue;
                    }

                    profile.Achievements.Add(achievement);

                    toasts.Add(new Toast
                    {
                        Header = achievement.Title,
                        Body = achievement.Content,
                        Award = GetAward(achievement.Rank)
                    });

                    profile.VirtualCoins += item.Award;

                    await context.SaveChangesAsync();
                }
            }

            return toasts;
        }

        private static decimal GetAward(int rank)
        {
            if (rank == 1)
            {
                return Parameters.BronzeBadgeAward;
            }
            else if (rank == 2)
            {
                return Parameters.SilverBadgeAward;
            }
            else
            {
                return Parameters.GoldBadgeAward;
            }
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
                    Header = AchievementNames.Witness
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
                    Header = AchievementNames.Gilgamesh
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
                    Header = AchievementNames.LongTimeAgo
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
                    Header = AchievementNames.Reader
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
                    Header = AchievementNames.Brainstorm
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
                    Header = AchievementNames.Rubicon
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
                        Header = AchievementNames.FirstGrade
                    });
            }

            if (result2 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SecondGrade
                    });
            }

            if (result3 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.ThirdGrade
                    });
            }

            if (result4 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.FourthGrade
                    });
            }

            if (result5 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.FifthGrade
                    });
            }

            if (result6 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SixthGrade
                    });
            }

            if (result7 > 0)
            {
                list.Add(
                    new Toast
                    {
                        Header = AchievementNames.SeventhGrade
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
                    Header = AchievementNames.Vote1
                });
            }

            if (voteCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote30
                });
            }

            if (voteCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote300
                });
            }

            if (voteCount > 599)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Vote600
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 發表創作次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> NovelCountAsync(ApplicationDbContext context, string profileId)
        {
            var novelCount = await context.Novels.Where(n => n.ProfileID == profileId).CountAsync();

            List<Toast> list = new();

            if (novelCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Novel1
                });
            }

            if (novelCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Novel30
                });
            }

            if (novelCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Novel100
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 發表章節次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> EpisodeCountAsync(ApplicationDbContext context, string profileId)
        {
            var episodeCount = await context.Episodes.Where(e => e.OwnerID == profileId).CountAsync();

            List<Toast> list = new();

            if (episodeCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Episode1
                });
            }

            if (episodeCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Episode30
                });
            }

            if (episodeCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Episode100
                });
            }

            if (episodeCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Episode300
                });
            }

            if (episodeCount > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Episode1000
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 發表文章次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> ArticleCountAsync(ApplicationDbContext context, string profileId)
        {
            var articleCount = await context.Articles.Where(e => e.ProfileID == profileId).CountAsync();

            List<Toast> list = new();

            if (articleCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article1
                });
            }

            if (articleCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article30
                });
            }

            if (articleCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article100
                });
            }

            if (articleCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article300
                });
            }

            if (articleCount > 599)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article600
                });
            }

            if (articleCount > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Article1000
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 發表意見次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> FeedbackCountAsync(ApplicationDbContext context, string profileId)
        {
            var feedbackCount = await context.Feedbacks.Where(n => n.OwnerID == profileId).CountAsync();

            List<Toast> list = new();

            if (feedbackCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Feedback1
                });
            }

            if (feedbackCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Feedback30
                });
            }

            if (feedbackCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Feedback100
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 發表評論次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> CommentCountAsync(ApplicationDbContext context, string profileId)
        {
            var commentCount = await context.Comments.Where(n => n.ProfileID == profileId).CountAsync();

            List<Toast> list = new();

            if (commentCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Comment1
                });
            }

            if (commentCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Comment30
                });
            }

            if (commentCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Comment300
                });
            }

            if (commentCount > 599)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Comment600
                });
            }

            if (commentCount > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Comment1000
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 意見被接受次數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        public static async Task<List<Toast>> AcceptCountAsync(ApplicationDbContext context, string profileId)
        {
            var feedbackCount = await context.Feedbacks.Where(n => n.OwnerID == profileId && n.Accept == true).CountAsync();

            List<Toast> list = new();

            if (feedbackCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Accept1
                });
            }

            if (feedbackCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Accept30
                });
            }

            if (feedbackCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Accept100
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 獲得InCoins
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins"></param>
        /// <returns></returns>
        public static async Task<List<Toast>> GetCoinsCountAsync(ApplicationDbContext context, string profileId, decimal coins)
        {
            List<Toast> list = new();

            if (coins > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetCoin1
                });
            }

            if (coins > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetCoin1000
                });
            }

            if (coins > 9999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetCoin10000
                });
            }

            if (coins > 99999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetCoin100000
                });
            }

            if (coins > 666665)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetCoin666666
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 使用InCoins
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins"></param>
        /// <returns></returns>
        public static async Task<List<Toast>> UseCoinsCountAsync(ApplicationDbContext context, string profileId, decimal coins)
        {
            List<Toast> list = new();

            if (coins > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin1
                });
            }

            if (coins > 499)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin500
                });
            }

            if (coins > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin1000
                });
            }

            if (coins > 9999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin10000
                });
            }

            if (coins > 29999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin30000
                });
            }

            if (coins > 77776)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.UseCoin77777
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 計算使用讚數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins"></param>
        /// <returns></returns>
        public static async Task<List<Toast>> AgreeCountAsync(ApplicationDbContext context, string profileId)
        {
            var agreeCount = await context.Agrees
                .Where(a => a.ProfileID == profileId)
                .CountAsync();

            List<Toast> list = new();

            if (agreeCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Agree1
                });
            }

            if (agreeCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Agree30
                });
            }

            if (agreeCount > 99)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Agree100
                });
            }

            if (agreeCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Agree300
                });
            }

            if (agreeCount > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.Agree1000
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }

        /// <summary>
        /// 計算獲得讚數
        /// </summary>
        /// <param name="context">DB context</param>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins"></param>
        /// <returns></returns>
        public static async Task<List<Toast>> GetAgreeCountAsync(ApplicationDbContext context, string profileId)
        {
            var totalCount = await context.Agrees
                .Where(a => (a.Comment.ProfileID == profileId || a.Message.ProfileID == profileId)
                && a.ProfileID != profileId)
                .CountAsync();

            List<Toast> list = new();

            if (totalCount > 0)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetAgree1
                });
            }

            if (totalCount > 29)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetAgree30
                });
            }

            if (totalCount > 299)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetAgree300
                });
            }

            if (totalCount > 599)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetAgree600
                });
            }

            if (totalCount > 999)
            {
                list.Add(new Toast
                {
                    Header = AchievementNames.GetAgree1000
                });
            }

            var toasts = await GetToasts(context, profileId, list);

            return toasts;
        }
    }
}