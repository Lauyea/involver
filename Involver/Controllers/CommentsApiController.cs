using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels.Api;
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers;

[Route("api/v1/comments")]
[ApiController]
public class CommentsApiController(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAuthorizationService authorizationService,
    INotificationSetter notificationSetter,
    IAchievementService achievementService,
    IMissionService missionService) : ControllerBase
{

    // GET: api/Comments
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCommentsAsync([FromQuery] string from, [FromQuery] int fromID, [FromQuery] int page = 1, [FromQuery] string sortBy = "oldest", [FromQuery] string ownerId = null, [FromQuery] bool authorOnly = false)
    {
        IQueryable<Comment> commentsQuery = context.Comments
            .Include(c => c.Profile)
            .Include(c => c.Dices);

        bool isCommentOrderFixed = false;

        // 留言所屬小說，用來查留言作者對這部小說的 Involve 資訊（文章留言不顯示）
        int? involvingNovelId = null;

        // 只取需要的欄位，不載入文章或章節全文
        switch (from.ToLower())
        {
            case "articles":
                isCommentOrderFixed = await context.Articles
                    .Where(a => a.ArticleID == fromID)
                    .Select(a => a.IsCommentOrderFixed)
                    .FirstOrDefaultAsync();
                commentsQuery = commentsQuery.Where(c => c.ArticleID == fromID);
                break;
            case "novels":
                isCommentOrderFixed = await context.Novels
                    .Where(n => n.NovelID == fromID)
                    .Select(n => n.IsCommentOrderFixed)
                    .FirstOrDefaultAsync();
                involvingNovelId = fromID;
                commentsQuery = commentsQuery.Where(c => c.NovelID == fromID);
                break;
            case "episodes":
                var episode = await context.Episodes
                    .Where(e => e.EpisodeID == fromID)
                    .Select(e => new { e.IsCommentOrderFixed, e.NovelID })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                if (episode != null)
                {
                    isCommentOrderFixed = episode.IsCommentOrderFixed;
                    involvingNovelId = episode.NovelID;
                }
                commentsQuery = commentsQuery.Where(c => c.EpisodeID == fromID);
                break;
            default:
                return BadRequest("Invalid 'from' parameter.");
        }

        if (authorOnly && !string.IsNullOrEmpty(ownerId))
        {
            commentsQuery = commentsQuery.Where(c => c.ProfileID == ownerId);
        }

        if (isCommentOrderFixed)
        {
            sortBy = "oldest";
        }

        commentsQuery = sortBy.ToLower() switch
        {
            "newest" => commentsQuery.OrderByDescending(c => c.CommentID),
            "most_agrees" => commentsQuery.OrderByDescending(c => c.Agrees.Count).ThenBy(c => c.CommentID),
            _ => commentsQuery.OrderBy(c => c.CommentID),
        };
        var paginatedComments = await PaginatedList<Comment>.CreateAsync(commentsQuery.AsNoTracking(), page, Parameters.CommetPageSize);

        var commentIds = paginatedComments.Select(c => c.CommentID).ToList();
        var profileIds = paginatedComments.Select(c => c.ProfileID).Distinct().ToList();

        var users = await context.Users.Where(u => profileIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

        var messageCounts = await context.Messages
            .Where(m => commentIds.Contains(m.CommentID))
            .GroupBy(m => m.CommentID)
            .Select(g => new { CommentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CommentId, x => x.Count);

        // 按讚數在資料庫端計算，不載入整個 Agrees 集合
        var agreeCounts = await context.Agrees
            .Where(a => a.CommentID != null && commentIds.Contains(a.CommentID.Value))
            .GroupBy(a => a.CommentID.Value)
            .Select(g => new { CommentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CommentId, x => x.Count);

        var currentUserID = userManager.GetUserId(User);

        var agreedCommentIds = new HashSet<int>();

        if (currentUserID != null)
        {
            var agreedIds = await context.Agrees
                .Where(a => a.ProfileID == currentUserID
                    && a.CommentID != null
                    && commentIds.Contains(a.CommentID.Value))
                .Select(a => a.CommentID.Value)
                .ToListAsync();

            agreedCommentIds = [.. agreedIds];
        }

        // 只查這一頁留言作者對這部小說的 Involve 紀錄，不載入整部小說的所有 Involver
        var involverInfos = new Dictionary<string, Involving>();

        if (involvingNovelId != null)
        {
            var involvings = await context.Involvings
                .Where(i => i.NovelID == involvingNovelId && profileIds.Contains(i.InvolverID))
                .OrderBy(i => i.InvolvingID)
                .AsNoTracking()
                .ToListAsync();

            foreach (var involving in involvings)
            {
                involverInfos.TryAdd(involving.InvolverID, involving);
            }
        }

        var commentDtos = new List<CommentDto>();

        foreach (var comment in paginatedComments)
        {
            var isOwner = currentUserID != null && currentUserID == comment.ProfileID;
            var canEdit = (await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update)).Succeeded;
            var canDelete = (await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Delete)).Succeeded;
            var canBlock = (await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded;

            string involverInfoString = null;

            if (involverInfos.TryGetValue(comment.ProfileID, out var involverInfo))
            {
                involverInfoString = $"月 Involve: {involverInfo.MonthlyValue:0.##} In幣 | 總 Involve: {involverInfo.TotalValue:0.##} In幣";
            }

            users.TryGetValue(comment.ProfileID, out var user);

            commentDtos.Add(new CommentDto
            {
                CommentID = comment.CommentID,
                Content = CustomHtmlSanitizer.SanitizeHtml(comment.Content.Replace("\r\n", "<br />")),
                UpdateTime = TimePeriodHelper.Get(comment.UpdateTime),
                FullUpdateTime = comment.UpdateTime.ToString("yyyy/MM/dd HH:mm:ss"),
                ProfileID = comment.ProfileID,
                UserName = comment.Profile.UserName,
                UserImageUrl = !string.IsNullOrEmpty(comment.Profile.ImageUrl)
                    ? comment.Profile.ImageUrl
                    : $"https://www.gravatar.com/avatar/{StringExtensions.ToMd5(user.Email)}?d=retro",
                InvolverInfo = involverInfoString,
                Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? [],
                MessagesCount = messageCounts.TryGetValue(comment.CommentID, out var count) ? count : 0,
                AgreesCount = agreeCounts.TryGetValue(comment.CommentID, out var agreeCount) ? agreeCount : 0,
                IsAgreedByCurrentUser = agreedCommentIds.Contains(comment.CommentID),
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanBlock = canBlock,
                IsBlocked = comment.Block
            });
        }

        var paginationMetadata = new
        {
            paginatedComments.TotalPages,
            paginatedComments.PageIndex,
            paginatedComments.HasPreviousPage,
            paginatedComments.HasNextPage
        };

        Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(paginationMetadata));

        return Ok(commentDtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommentAsync([FromBody] CreateCommentDto createDto)
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }
        if (await context.IsBannedAsync(user.Id))
        {
            return Forbid();
        }

        var comment = new Comment
        {
            Content = createDto.Content,
            ProfileID = user.Id,
            UpdateTime = System.DateTime.Now
        };

        var isAuthorized = await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Create);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        switch (createDto.From.ToLower())
        {
            case "articles":
                comment.ArticleID = createDto.FromID;
                break;
            case "novels":
                comment.NovelID = createDto.FromID;
                break;
            case "episodes":
                comment.EpisodeID = createDto.FromID;
                break;
            default:
                return BadRequest("Invalid 'from' parameter.");
        }

        // Dice roll logic
        comment.Dices = [];
        bool hasDices = false;

        // Handle UI-based dice rolls
        if (createDto.RollTimes > 0 && createDto.DiceSides > 0 && createDto.From.Equals("episode", StringComparison.CurrentCultureIgnoreCase))
        {
            hasDices = true;
            Random random = new();
            for (int i = 0; i < createDto.RollTimes; i++)
            {
                comment.Dices.Add(new Dice
                {
                    Sides = createDto.DiceSides,
                    Value = random.Next(1, createDto.DiceSides + 1)
                });
            }

            // Replace DiceTotal placeholder
            int diceTotal = comment.Dices.Sum(d => d.Value);
            string diceTotalString = $"UI's DiceTotal: {diceTotal}";
            comment.Content = comment.Content.Replace("DiceTotal", diceTotalString);
        }

        // Handle text-based dice rolls
        string contentWithDiceRolls = comment.Content;
        int textDiceCount = DiceHelper.ReplaceRollDiceString(ref contentWithDiceRolls);
        comment.Content = contentWithDiceRolls;

        // 檢查並移除使用者自行輸入的骰子資訊標籤
        string textToSanitize = comment.Content;

        // 支援 class="dice-roll-info" 或 class='dice-roll-info'，
        // 並允許有其他屬性
        var diceRollInfoTagPattern = new System.Text.RegularExpressions.Regex(
            @"<p[^>]*class\s*=\s*(""dice-roll-info""|'dice-roll-info')[^>]*>.*?<\/p>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

        comment.Content = diceRollInfoTagPattern.Replace(textToSanitize, "");


        if (textDiceCount > 0)
        {
            hasDices = true;
            // Add a placeholder to indicate text-based roll, as in original logic
            if (!comment.Dices.Any(d => d.Sides == 0) && comment.Dices.Count == 0)
            {
                comment.Dices.Add(new Dice { Sides = 0, Value = 0 });
            }

            comment.Content += $"<p class='dice-roll-info' data-dicecount='{textDiceCount}'>🎲 此評論有擲骰 {textDiceCount} 次</p>";
        }

        context.Comments.Add(comment);

        await context.SaveChangesAsync();

        var commenterProfile = await context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == user.Id);

        // Mission check：任務旗標與獎勵由任務服務原子更新，完成所有任務時會自動加獎勵幣
        // TODO: Add a way to notify user of mission completion
        await missionService.CompleteAsync(user.Id, MissionType.LeaveComment);

        //Set notification
        var url = $"{Request.Scheme}://{Request.Host}/{createDto.From}/Details/?id={createDto.FromID}#comment-{comment.CommentID}";
        await notificationSetter.ForCommentAsync(createDto.From, createDto.FromID, url, user.Id, comment.Content);

        // Achievement check and get achievement toasts
        var toasts = await achievementService.CommentCountAsync(user.Id);
        if (hasDices)
        {
            var diceToasts = await achievementService.RollDicesAsync(user.Id);
            toasts.AddRange(diceToasts);
        }

        // Calculate the new total pages
        IQueryable<Comment> commentsQuery = context.Comments;
        switch (createDto.From.ToLower())
        {
            case "articles":
                commentsQuery = commentsQuery.Where(c => c.ArticleID == createDto.FromID);
                break;
            case "novels":
                commentsQuery = commentsQuery.Where(c => c.NovelID == createDto.FromID);
                break;
            case "episodes":
                commentsQuery = commentsQuery.Where(c => c.EpisodeID == createDto.FromID);
                break;
        }
        var totalComments = await commentsQuery.CountAsync();
        var newTotalPages = (int)Math.Ceiling(totalComments / (double)Parameters.CommetPageSize);

        // Map to DTO to return
        var newCommentDto = new CommentDto
        {
            CommentID = comment.CommentID,
            Content = CustomHtmlSanitizer.SanitizeHtml(comment.Content.Replace("\n", "<br />")),
            UpdateTime = TimePeriodHelper.Get(comment.UpdateTime),
            FullUpdateTime = comment.UpdateTime.ToString("yyyy/MM/dd HH:mm:ss"),
            ProfileID = comment.ProfileID,
            UserName = commenterProfile.UserName,
            UserImageUrl = !string.IsNullOrEmpty(commenterProfile.ImageUrl)
                ? commenterProfile.ImageUrl
                : $"https://www.gravatar.com/avatar/{StringExtensions.ToMd5(user.Email)}?d=retro",
            InvolverInfo = null, // To be implemented
            Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? [],
            MessagesCount = 0, // New comment has no messages
            AgreesCount = 0,
            IsAgreedByCurrentUser = false,
            CanEdit = true, // User can always edit their own new comment
            CanDelete = true, // User can always delete their own new comment
            CanBlock = (await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded,
            IsBlocked = comment.Block
        };

        var response = new
        {
            Comment = newCommentDto,
            NewTotalPages = newTotalPages,
            Toasts = toasts
        };

        return CreatedAtAction(nameof(GetCommentsAsync), new { from = createDto.From, fromID = createDto.FromID }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommentAsync(int id, [FromBody] UpdateCommentDto updateDto)
    {
        var comment = await context.Comments.FindAsync(id);

        if (comment == null)
        {
            return NotFound();
        }

        var isAuthorized = await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update);
        if (!isAuthorized.Succeeded || comment.Block)
        {
            return Forbid();
        }

        comment.Content = CustomHtmlSanitizer.SanitizeHtml(updateDto.Content);
        comment.UpdateTime = System.DateTime.Now;

        context.Entry(comment).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!context.Comments.Any(e => e.CommentID == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var toasts = await achievementService.FirstTimeEditAsync(comment.ProfileID);

        return Ok(new { Toasts = toasts });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommentAsync(int id)
    {
        var comment = await context.Comments.FindAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        var isAuthorized = await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        // Agrees 對留言與回覆的 FK 是 NO ACTION（避免與 Profile 形成多重串聯刪除路徑），
        // 要先刪掉這則留言及其回覆的按讚，否則有人按過讚的留言會刪除失敗
        await using var transaction = await context.Database.BeginTransactionAsync();

        await context.Agrees
            .Where(a => a.CommentID == id || a.Message.CommentID == id)
            .ExecuteDeleteAsync();

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    [HttpPost("{id}/agree")]
    public async Task<IActionResult> ToggleAgreeAsync(int id)
    {
        var currentUserID = userManager.GetUserId(User);
        if (currentUserID == null)
        {
            return Unauthorized();
        }

        var comment = await context.Comments
            .FirstOrDefaultAsync(c => c.CommentID == id);

        if (comment == null)
        {
            return NotFound();
        }

        // 只查自己那一筆，不載入這則留言的全部 Agree
        var existingAgree = await context.Agrees
            .FirstOrDefaultAsync(a => a.CommentID == id && a.ProfileID == currentUserID);

        if (existingAgree != null)
        {
            // Un-agree
            context.Agrees.Remove(existingAgree);
            await context.SaveChangesAsync();

            return Ok(new { agreesCount = await CountAgreesAsync(id), Toasts = new List<Toast>() });
        }

        // Agree
        var agree = new Agree
        {
            CommentID = id,
            ProfileID = currentUserID,
            UpdateTime = System.DateTime.Now
        };
        context.Agrees.Add(agree);

        try
        {
            await context.SaveChangesAsync(); // Save agree first
        }
        catch (DbUpdateException)
        {
            // 連點時另一個請求已經先按讚，Unique 索引擋下重複的那筆，結果仍是「已按讚」
            context.Entry(agree).State = EntityState.Detached;
            return Ok(new { agreesCount = await CountAgreesAsync(id), Toasts = new List<Toast>() });
        }

        // Check mission and achievements for the comment owner
        if (comment.ProfileID != currentUserID)
        {
            var ownerExists = await context.Profiles
                .AnyAsync(p => p.ProfileID == comment.ProfileID);

            if (ownerExists)
            {
                // 任務旗標與獎勵由任務服務原子更新，完成所有任務時會自動加獎勵幣
                await missionService.CompleteAsync(comment.ProfileID, MissionType.BeAgreed);

                var toasts = await achievementService.GetAgreeCountAsync(comment.ProfileID);

                // Set notification
                var from = GetCommentSource(comment);
                var fromId = GetCommentSourceId(comment);
                var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromId}#comment-{comment.CommentID}";
                await notificationSetter.ForCommentBeAgreedAsync(comment.Content, comment.ProfileID, url, toasts);
            }
        }

        // Check achievements for user
        List<Toast> userToasts = await achievementService.AgreeCountAsync(currentUserID);

        await context.SaveChangesAsync();

        return Ok(new { agreesCount = await CountAgreesAsync(id), Toasts = userToasts });
    }

    /// <summary>
    /// 計算留言的按讚數
    /// </summary>
    /// <param name="commentId">CommentID</param>
    /// <returns>按讚數</returns>
    private Task<int> CountAgreesAsync(int commentId)
    {
        return context.Agrees.CountAsync(a => a.CommentID == commentId);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> ToggleBlockAsync(int id)
    {
        var comment = await context.Comments.FindAsync(id);
        if (comment == null)
        {
            return NotFound();
        }

        var isAuthorized = await authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        comment.Block = !comment.Block;
        context.Entry(comment).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Ok(new { isBlocked = comment.Block });
    }

    // API Endpoints will be implemented here
    private string GetCommentSource(Comment comment)
    {
        if (comment.ArticleID != null) return Parameters.Articles;
        if (comment.NovelID != null) return Parameters.Novels;
        if (comment.EpisodeID != null) return Parameters.Episodes;
        return string.Empty;
    }

    private int? GetCommentSourceId(Comment comment)
    {
        if (comment.ArticleID != null) return comment.ArticleID;
        if (comment.NovelID != null) return comment.NovelID;
        if (comment.EpisodeID != null) return comment.EpisodeID;
        return null;
    }
}