namespace Involver.Services;

/// <summary>
/// 任務種類。名稱必須與 <see cref="DataAccess.Models.Missions"/> 的屬性名稱相同
/// </summary>
public enum MissionType
{
    /// <summary>看一篇文章</summary>
    WatchArticle,

    /// <summary>投一次票</summary>
    Vote,

    /// <summary>留一則評論</summary>
    LeaveComment,

    /// <summary>瀏覽公告</summary>
    ViewAnnouncement,

    /// <summary>分享一次創作</summary>
    ShareCreation,

    /// <summary>被認同一次</summary>
    BeAgreed,

    /// <summary>每日登入</summary>
    DailyLogin,
}

/// <summary>
/// 任務服務：標記任務完成並透過 <see cref="ICoinService"/> 發放虛擬 In 幣獎勵。
/// 任務旗標以原子更新設定，同一個任務併發完成也只會發一次獎勵。
/// </summary>
public interface IMissionService
{
    /// <summary>
    /// 完成任務並發放獎勵。若因此完成所有每週任務，再額外發放全勤獎勵。
    /// 任務旗標直接寫入資料庫，不會改動呼叫端已追蹤的 Missions 實體。
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="mission">任務種類</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>這次呼叫才完成任務回傳 true；任務先前已完成或找不到使用者回傳 false</returns>
    Task<bool> CompleteAsync(string profileId, MissionType mission, CancellationToken cancellationToken = default);
}
