using System.Text.Json;

using DataAccess.Data;

using Involver.Authorization.Profile;
using Involver.Common;
using Involver.Models.ViewModels.Feed;
using Involver.Models.ViewModels.Profiles;
using Involver.Services;
using Involver.Services.Profiles;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 個人檔案總覽：自我介紹、最新作品；本人另外看到繼續閱讀、每週任務進度與帳號捷徑
/// </summary>
[AllowAnonymous]
public class IndexModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    IViewCountService viewCountService,
    IProfileReadingService readingService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 總覽顯示幾張最新作品
    /// </summary>
    public const int LatestWorkCount = 4;

    /// <summary>
    /// 總覽的繼續閱讀最多顯示幾部
    /// </summary>
    public const int ContinueReadingCount = 8;

    /// <summary>
    /// 每週任務的項目數（不含「完成其他任務」）
    /// </summary>
    public const int MissionCount = 7;

    /// <summary>
    /// 個人檔案
    /// </summary>
    public DataAccess.Models.Profile Profile { get; set; }

    /// <summary>
    /// 瀏覽者是否為本人
    /// </summary>
    public bool ProfileOwner { get; set; }

    /// <summary>
    /// 最近更新的作品與文章
    /// </summary>
    public List<ContentCardViewModel> LatestWorks { get; set; } = [];

    /// <summary>
    /// 本人：閱讀中的作品（從書架取前幾部）
    /// </summary>
    public List<BookshelfItemViewModel> ContinueReading { get; set; } = [];

    /// <summary>
    /// 本人：書架上是否有作品（決定繼續閱讀區塊的空狀態文字）
    /// </summary>
    public bool HasBookshelf { get; set; }

    /// <summary>
    /// 已達成的成就數
    /// </summary>
    public int AchievementCount { get; set; }

    /// <summary>
    /// 本人：本週完成的任務數
    /// </summary>
    public int MissionsDone { get; set; }

    /// <summary>
    /// 載入個人檔案（含每週任務）並判斷瀏覽者是否為本人
    /// </summary>
    /// <param name="id">ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>找到會員時為 true</returns>
    private async Task<bool> LoadProfileAsync(string id, CancellationToken cancellationToken)
    {
        Profile = await Context.Profiles
            .Include(p => p.Missions)
            .FirstOrDefaultAsync(p => p.ProfileID == id, cancellationToken);
        ProfileOwner = Profile != null && UserManager.GetUserId(User) == id;
        return Profile != null;
    }

    /// <summary>
    /// 顯示總覽
    /// </summary>
    /// <param name="id">ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；找不到會員時 404</returns>
    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadProfileAsync(id, cancellationToken))
        {
            return NotFound();
        }

        // 觀看數以原子更新累加，避免併發時「讀出、改值、存回」掉計數
        await viewCountService.AddProfileViewAsync(Profile);

        LatestWorks = await readingService.GetLatestWorksAsync(id, LatestWorkCount, cancellationToken);
        AchievementCount = await Context.Profiles
            .Where(p => p.ProfileID == id)
            .Select(p => p.Achievements!.Count)
            .FirstOrDefaultAsync(cancellationToken);

        if (ProfileOwner)
        {
            var bookshelf = await readingService.GetBookshelfAsync(id, cancellationToken);
            HasBookshelf = bookshelf.Count > 0;
            ContinueReading = bookshelf
                .Where(b => b.Status == BookshelfStatus.Reading)
                .OrderByDescending(b => b.LastReadAt)
                .Take(ContinueReadingCount)
                .ToList();

            var missions = Profile.Missions;
            MissionsDone = missions == null ? 0 : new[]
            {
                missions.WatchArticle, missions.Vote, missions.LeaveComment, missions.ViewAnnouncement,
                missions.ShareCreation, missions.BeAgreed, missions.DailyLogin,
            }.Count(done => done);

            // 成就與 Toast 只屬於本人：造訪別人的頁面時不可替對方檢查成就，否則 Toast 會顯示在造訪者畫面上
            var toasts = await AchievementService.GetCoinsCountAsync(Profile.ProfileID, Profile.VirtualCoins + Profile.RealCoins);
            toasts.AddRange(await AchievementService.CheckGradeAsync(Profile.ProfileID, Profile.EnrollmentDate));
            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }
        }

        return Page();
    }

    /// <summary>
    /// 封鎖或解除封鎖（頂部的按鈕送到這裡）
    /// </summary>
    /// <param name="id">ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到總覽</returns>
    public async Task<IActionResult> OnPostAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadProfileAsync(id, cancellationToken))
        {
            return NotFound();
        }

        // 畫面只對有權限的人顯示按鈕，伺服器端也必須檢查，否則任何人都能直接 POST 封鎖別人
        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, Profile, ProfileOperations.Ban);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        // 封鎖狀態以 Profiles.Banned 為唯一來源
        Profile.Banned = !Profile.Banned;
        await Context.SaveChangesAsync(cancellationToken);
        StatusMessage = Profile.Banned ? "已封鎖這位會員。" : "已解除封鎖。";

        return RedirectToPage(new { id });
    }
}
