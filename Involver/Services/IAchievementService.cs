using System.Collections.Generic;
using System.Threading.Tasks;
using Involver.Common;

namespace Involver.Services
{
    public interface IAchievementService
    {
        /// <summary>
        /// Beta Involver
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> BeBetaInvolverAsync(string profileId);

        /// <summary>
        /// 成為駐站作家
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> BeProfessionalAsync(string profileId);

        /// <summary>
        /// 第一次編輯
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> FirstTimeEditAsync(string profileId);


        /// <summary>
        /// 第一次刪除
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> FirstTimeDeleteAsync(string profileId);

        /// <summary>
        /// 第一次填寫自我介紹
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> BeAutobiographerAsync(string profileId);

        /// <summary>
        /// 第一次舉辦投票
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> FirstTimeCreateVotingAsync(string profileId);

        /// <summary>
        /// 第一次使用標籤
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> FirstTimeUseTagsAsync(string profileId);

        /// <summary>
        /// 第一次閱讀公告
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ReadAnnouncementAsync(string profileId);

        /// <summary>
        /// 第一次閱讀創作
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ReadNovelAsync(string profileId);

        /// <summary>
        /// 第一次閱讀章節
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ReadEpisodeAsync(string profileId);

        /// <summary>
        /// 第一次閱讀文章
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ReadArticleAsync(string profileId);

        /// <summary>
        /// 第一次閱讀意見與回饋
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ReadFeedbackAsync(string profileId);

        /// <summary>
        /// 第一次擲骰
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> RollDicesAsync(string profileId);

        /// <summary>
        /// 註冊時間成就
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="enrollmentDate">註冊時間</param>
        /// <returns></returns>
        Task<List<Toast>> CheckGradeAsync(string profileId, DateTime enrollmentDate);

        /// <summary>
        /// 投票次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> VoteCountAsync(string profileId);

        /// <summary>
        /// 發表創作次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> NovelCountAsync(string profileId);

        /// <summary>
        /// 發表章節次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> EpisodeCountAsync(string profileId);

        /// <summary>
        /// 發表文章次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> ArticleCountAsync(string profileId);

        /// <summary>
        /// 發表意見次數
        /// </summary>
        /// <param name="profileId"></param>
        /// <returns></returns>
        Task<List<Toast>> FeedbackCountAsync(string profileId);

        /// <summary>
        /// 發表評論次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> CommentCountAsync(string profileId);

        /// <summary>
        /// 意見被接受次數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> AcceptCountAsync(string profileId);

        /// <summary>
        /// 獲得InCoins
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins">InCoins</param>
        /// <returns></returns>
        Task<List<Toast>> GetCoinsCountAsync(string profileId, decimal coins);

        /// <summary>
        /// 使用InCoins
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <param name="coins">InCoins</param>
        /// <returns></returns>
        Task<List<Toast>> UseCoinsCountAsync(string profileId, decimal coins);

        /// <summary>
        /// 計算使用讚數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> AgreeCountAsync(string profileId);

        /// <summary>
        /// 計算獲得讚數
        /// </summary>
        /// <param name="profileId">Profile ID</param>
        /// <returns></returns>
        Task<List<Toast>> GetAgreeCountAsync(string profileId);
    }
}
