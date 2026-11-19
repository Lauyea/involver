using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Comment;
using Involver.Authorization.Voting;
using Involver.Common;
using Involver.Extensions;
using Involver.Services;
using Involver.Services.Recommendations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using ReverseMarkdown;

namespace Involver.Pages.Episodes;

[AllowAnonymous]
public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
IViewCountService viewCountService,
IMissionService missionService,
IEpisodeReadService episodeReadService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Episode Episode { get; set; }

    public Episode PreviousEpisode { get; set; }

    public Episode NextEpisode { get; set; }

    public Novel Novel { get; set; }

    public List<Voting> Votings { get; set; }

    public string UserID { get; set; }

    public bool CanCreateVoting { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
    {
        UserID = UserManager.GetUserId(User);

        if (id == null)
        {
            return NotFound();
        }

        Episode = await Context.Episodes
            .Include(e => e.Novel)
            .FirstOrDefaultAsync(m => m.EpisodeID == id);

        // 章節不存在時要先回 404，再讀 Episode.Novel
        if (Episode == null)
        {
            return NotFound();
        }

        Novel = Episode.Novel;

        PreviousEpisode = await Context.Episodes
            .Where(e => e.NovelID == Novel.NovelID)
            .Where(e => e.EpisodeID < id)
            .OrderByDescending(e => e.EpisodeID)
            .FirstOrDefaultAsync();

        NextEpisode = await Context.Episodes
            .Where(e => e.NovelID == Novel.NovelID)
            .Where(e => e.EpisodeID > id)
            .OrderBy(e => e.EpisodeID)
            .FirstOrDefaultAsync();

        Votings = await Context.Votings
            .Where(v => v.EpisodeID == id)
            .Include(v => v.NormalOptions)
            .ToListAsync();

        // 每個投票的總票數在資料庫端計算，不載入每一張票
        var votingIds = Votings.Select(v => v.VotingID).ToList();

        var voteCounts = await Context.Votes
            .Where(vote => vote.NormalOptionID != null && votingIds.Contains(vote.NormalOption.VotingID))
            .GroupBy(vote => vote.NormalOption.VotingID)
            .Select(g => new { VotingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VotingId, x => x.Count);

        foreach (Voting voting in Votings)
        {
            if (voting.NormalOptions.Count() == 0)
            {
                continue;
            }

            if (voting.Limit == LimitType.Time)
            {
                //限時已過，投票結束
                TimeSpan? date = voting.DeadLine - DateTime.Now;
                string dateString = date.ToString();
                if (dateString.StartsWith("-") && voting.End == false)
                {
                    voting.End = true;
                }
            }

            if (voting.Limit == LimitType.Number)
            {
                //限定票數已過，投票結束
                int TotalVotesCount = voteCounts.TryGetValue(voting.VotingID, out var count) ? count : 0;
                if (TotalVotesCount > voting.NumberLimit && voting.End == false)
                {
                    voting.End = true;
                }
            }

            if (voting.Limit == LimitType.Value)
            {
                //限定總值已過，投票結束
                if (voting.TotalCoins > voting.CoinLimit && voting.End == false)
                {
                    voting.End = true;
                }
            }
        }

        // 觀看數以原子更新累加，避免併發時「讀出、改值、存回」掉計數
        await viewCountService.AddEpisodeViewAsync(Episode);

        await CheckMissionWatchArticle();

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EpisodeExists(Episode.EpisodeID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        // 章節閱讀紀錄（讀完率）：進入章節記為開始閱讀，讀到結尾由前端另外回報
        if (UserID != null && !Episode.IsDeleted)
        {
            await episodeReadService.RecordOpenAsync(UserID, Episode.EpisodeID);
        }

        var toasts = await AchievementService.ReadEpisodeAsync(UserID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        if (Episode.OwnerID == UserID)
        {
            CanCreateVoting = true;
        }

        return Page();
    }

    private async Task CheckMissionWatchArticle()
    {
        //Check mission:WatchArticle
        string UserID = UserManager.GetUserId(User);
        if (UserID != null)
        {
            // 任務旗標與獎勵由任務服務原子更新，完成所有任務時會自動加獎勵幣
            if (await missionService.CompleteAsync(UserID, MissionType.WatchArticle))
            {
                StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
            }
        }
    }

    private bool EpisodeExists(int id)
    {
        return Context.Episodes.Any(e => e.EpisodeID == id);
    }

    public async Task<IActionResult> OnGetMarkdownAsync(int id)
    {
        var episode = await Context.Episodes.FindAsync(id);
        if (episode == null)
        {
            return NotFound();
        }

        // Authorization check
        var currentUser = await UserManager.GetUserAsync(User);
        if (currentUser == null || episode.OwnerID != currentUser.Id)
        {
            return Forbid();
        }

        // Fetch related data
        var fullEpisode = await Context.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EpisodeID == id);

        var comments = await Context.Comments
            .AsNoTracking()
            .Include(c => c.Profile)
            .Where(c => c.EpisodeID == id && c.ProfileID == episode.OwnerID)
            .OrderBy(c => c.CommentID)
            .ToListAsync();

        var commentIds = comments.Select(c => c.CommentID).ToList();

        var messages = await Context.Messages
            .AsNoTracking()
            .Include(m => m.Profile)
            .Where(m => commentIds.Contains(m.CommentID))
            .OrderBy(m => m.UpdateTime)
            .ToListAsync();

        // Markdown builder
        var converter = new Converter();
        var sb = new StringBuilder();

        sb.AppendLine($"# {fullEpisode.Title}");
        sb.AppendLine(converter.Convert(fullEpisode.Content));
        sb.AppendLine("\n---\n");

        sb.AppendLine("## 評論與留言");

        sb.AppendLine("");

        // 巢狀輸出
        foreach (var comment in comments)
        {
            sb.AppendLine($"### 評論");
            sb.AppendLine(converter.Convert(comment.Content));
            sb.AppendLine();

            // 找出屬於此 comment 的 messages
            var relatedMessages = messages
                .Where(m => m.CommentID == comment.CommentID)
                .OrderBy(m => m.UpdateTime)
                .ToList();

            if (relatedMessages.Count != 0)
            {
                sb.AppendLine("#### 回覆");
                foreach (var message in relatedMessages)
                {
                    sb.AppendLine($"{message.Profile.UserName}：{converter.Convert(message.Content)}");
                }
            }

            sb.AppendLine("\n---\n");
        }

        return Content(sb.ToString());
    }

}