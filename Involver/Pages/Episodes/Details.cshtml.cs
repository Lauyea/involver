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
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
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
            .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

        Novel = Episode.Novel;

        if (Episode == null)
        {
            return NotFound();
        }

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
                .ThenInclude(n => n.Votes)
            .ToListAsync();

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
                int TotalVotesCount = 0;
                foreach (var option in voting.NormalOptions)
                {
                    TotalVotesCount += option.Votes.Count();
                }
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

        //Add views
        Episode.Views++;

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
            Profile userProfile = await Context.Profiles
            .Where(p => p.ProfileID == UserID)
            .Include(p => p.Missions)
            .FirstOrDefaultAsync();
            if (userProfile.Missions.WatchArticle != true)
            {
                userProfile.Missions.WatchArticle = true;
                userProfile.AwardCoins();
                StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
            }

            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            userProfile.Missions.CheckCompletion(userProfile);
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