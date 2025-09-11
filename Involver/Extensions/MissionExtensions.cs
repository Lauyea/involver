using DataAccess.Common;
using DataAccess.Models;

namespace Involver.Extensions
{
    public static class MissionExtensions
    {
        /// <summary>
        /// 檢查是否完成所有其他任務，如果完成就設定 CompleteOtherMissions 並發獎勵幣
        /// </summary>
        /// <param name="missions">Missions 物件</param>
        /// <param name="profile">Profile 物件（用來加幣）</param>
        /// <param name="rewardCoins">完成所有任務的獎勵幣數</param>
        public static void CheckCompletion(this Missions missions, Profile profile, int rewardCoins = Parameters.MissionAwardCoins)
        {
            if (missions.WatchArticle
                && missions.Vote
                && missions.LeaveComment
                && missions.ViewAnnouncement
                && missions.ShareCreation
                && missions.BeAgreed)
            {
                if (!missions.CompleteOtherMissions) // 確保只發一次獎勵
                {
                    missions.CompleteOtherMissions = true;
                    profile.AwardCoins(rewardCoins);
                }
            }
        }

        /// <summary>
        /// 發幣
        /// </summary>
        public static void AwardCoins(this Profile profile, int coins = Parameters.MissionAwardCoins)
        {
            profile.VirtualCoins += coins;
        }
    }

}
