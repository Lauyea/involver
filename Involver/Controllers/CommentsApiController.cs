using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels.Api;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("api/v1/comments")]
    [ApiController]
    public class CommentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationSetter _notificationSetter;

        public CommentsApiController(
            ApplicationDbContext context,
            UserManager<InvolverUser> userManager,
            IAuthorizationService authorizationService,
            INotificationSetter notificationSetter)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _notificationSetter = notificationSetter;
        }

        // GET: api/Comments
        [HttpGet]
        public async Task<IActionResult> GetCommentsAsync([FromQuery] string from, [FromQuery] int fromID, [FromQuery] int page = 1, [FromQuery] string sortBy = "oldest", [FromQuery] string ownerId = null, [FromQuery] bool authorOnly = false)
        {
            IQueryable<Comment> commentsQuery = _context.Comments
                .Include(c => c.Profile)
                .Include(c => c.Agrees)
                .Include(c => c.Dices);

            bool isCommentOrderFixed = false;

            switch (from.ToLower())
            {
                case "articles":
                    var article = await _context.Articles.FindAsync(fromID);
                    if (article != null)
                    {
                        isCommentOrderFixed = article.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery.Where(c => c.ArticleID == fromID);
                    break;
                case "novels":
                    var novel = await _context.Novels.FindAsync(fromID);
                    if (novel != null)
                    {
                        isCommentOrderFixed = novel.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery
                        .Include(c => c.Novel.Involvers)
                        .Where(c => c.NovelID == fromID);
                    break;
                case "episodes":
                    var episode = await _context.Episodes.FindAsync(fromID);
                    if (episode != null)
                    {
                        isCommentOrderFixed = episode.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery
                        .Include(c => c.Episode.Novel.Involvers)
                        .Where(c => c.EpisodeID == fromID);
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

            switch (sortBy.ToLower())
            {
                case "newest":
                    commentsQuery = commentsQuery.OrderByDescending(c => c.CommentID);
                    break;
                case "most_agrees":
                    commentsQuery = commentsQuery.OrderByDescending(c => c.Agrees.Count).ThenBy(c => c.CommentID);
                    break;
                case "oldest":
                default:
                    commentsQuery = commentsQuery.OrderBy(c => c.CommentID);
                    break;
            }

            var paginatedComments = await PaginatedList<Comment>.CreateAsync(commentsQuery.AsNoTracking(), page, Parameters.CommetPageSize);

            var commentIds = paginatedComments.Select(c => c.CommentID).ToList();
            var profileIds = paginatedComments.Select(c => c.ProfileID).Distinct().ToList();

            var users = await _context.Users.Where(u => profileIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var messageCounts = await _context.Messages
                .Where(m => commentIds.Contains(m.CommentID))
                .GroupBy(m => m.CommentID)
                .Select(g => new { CommentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CommentId, x => x.Count);

            var currentUserID = _userManager.GetUserId(User);

            var commentDtos = new List<CommentDto>();

            foreach (var comment in paginatedComments)
            {
                var isOwner = currentUserID != null && currentUserID == comment.ProfileID;
                var canEdit = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update)).Succeeded;
                var canDelete = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Delete)).Succeeded;
                var canBlock = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded;

                string involverInfoString = null;
                Involving involverInfo = null;

                if (comment.EpisodeID != null && comment.Episode?.Novel?.Involvers != null)
                {
                    involverInfo = comment.Episode.Novel.Involvers.FirstOrDefault(i => i.InvolverID == comment.ProfileID);
                }
                else if (comment.NovelID != null && comment.Novel?.Involvers != null)
                {
                    involverInfo = comment.Novel.Involvers.FirstOrDefault(i => i.InvolverID == comment.ProfileID);
                }

                if (involverInfo != null)
                {
                    involverInfoString = $"月 Involve: {involverInfo.MonthlyValue} In幣 | 總 Involve: {involverInfo.TotalValue} In幣";
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
                    Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? new List<object>(),
                    MessagesCount = messageCounts.TryGetValue(comment.CommentID, out var count) ? count : 0,
                    AgreesCount = comment.Agrees.Count,
                    IsAgreedByCurrentUser = currentUserID != null && comment.Agrees.Any(a => a.ProfileID == currentUserID),
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
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }
            if (user.Banned)
            {
                return Forbid();
            }

            var comment = new Comment
            {
                Content = createDto.Content,
                ProfileID = user.Id,
                UpdateTime = System.DateTime.Now
            };

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Create);
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
            comment.Dices = new List<Dice>();
            bool hasDices = false;

            // Handle UI-based dice rolls
            if (createDto.RollTimes > 0 && createDto.DiceSides > 0 && createDto.From.ToLower() == "episode")
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

            _context.Comments.Add(comment);

            // Mission check
            var commenterProfile = await _context.Profiles.Include(p => p.Missions).FirstOrDefaultAsync(p => p.ProfileID == user.Id);
            if (commenterProfile != null && commenterProfile.Missions.LeaveComment != true)
            {
                commenterProfile.Missions.LeaveComment = true;
                commenterProfile.AwardCoins();
                // TODO: Add a way to notify user of mission completion
            }

            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            commenterProfile.Missions.CheckCompletion(commenterProfile);

            await _context.SaveChangesAsync();

            //Set notification
            var url = $"{Request.Scheme}://{Request.Host}/{createDto.From}/Details/?id={createDto.FromID}#comment-{comment.CommentID}";
            await _notificationSetter.ForCommentAsync(createDto.From, createDto.FromID, url, user.Id, comment.Content);

            // Achievement check and get achievement toasts
            var toasts = await AchievementHelper.CommentCountAsync(_context, user.Id);
            if (hasDices)
            {
                var diceToasts = await AchievementHelper.RollDicesAsync(_context, user.Id);
                toasts.AddRange(diceToasts);
            }

            // Calculate the new total pages
            IQueryable<Comment> commentsQuery = _context.Comments;
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
                Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? new List<object>(),
                MessagesCount = 0, // New comment has no messages
                AgreesCount = 0,
                IsAgreedByCurrentUser = false,
                CanEdit = true, // User can always edit their own new comment
                CanDelete = true, // User can always delete their own new comment
                CanBlock = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded,
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
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update);
            if (!isAuthorized.Succeeded || comment.Block)
            {
                return Forbid();
            }

            comment.Content = CustomHtmlSanitizer.SanitizeHtml(updateDto.Content);
            comment.UpdateTime = System.DateTime.Now;

            _context.Entry(comment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Comments.Any(e => e.CommentID == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            var toasts = await AchievementHelper.FirstTimeEditAsync(_context, comment.ProfileID);

            return Ok(new { Toasts = toasts });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommentAsync(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/agree")]
        public async Task<IActionResult> ToggleAgreeAsync(int id)
        {
            var currentUserID = _userManager.GetUserId(User);
            if (currentUserID == null)
            {
                return Unauthorized();
            }

            var comment = await _context.Comments
                .Include(c => c.Profile)
                .Include(c => c.Agrees)
                .FirstOrDefaultAsync(c => c.CommentID == id);

            if (comment == null)
            {
                return NotFound();
            }

            List<Toast> userToasts = [];

            var existingAgree = comment.Agrees.FirstOrDefault(a => a.ProfileID == currentUserID);

            if (existingAgree == null)
            {
                // Agree
                _context.Agrees.Add(new Agree
                {
                    CommentID = id,
                    ProfileID = currentUserID,
                    UpdateTime = System.DateTime.Now
                });

                // Check mission and achievements for the comment owner
                if (comment.ProfileID != currentUserID)
                {
                    var ownerProfile = await _context.Profiles
                        .Include(p => p.Missions)
                        .FirstOrDefaultAsync(p => p.ProfileID == comment.ProfileID);

                    if (ownerProfile != null)
                    {
                        if (ownerProfile.Missions.BeAgreed != true)
                        {
                            ownerProfile.Missions.BeAgreed = true;
                            ownerProfile.AwardCoins();
                        }

                        // 檢查是否完成所有任務，若完成會自動加獎勵幣
                        ownerProfile.Missions.CheckCompletion(ownerProfile);

                        await _context.SaveChangesAsync(); // Save mission changes first

                        var toasts = await AchievementHelper.GetAgreeCountAsync(_context, ownerProfile.ProfileID);

                        // Set notification
                        var from = GetCommentSource(comment);
                        var fromId = GetCommentSourceId(comment);
                        var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromId}#comment-{comment.CommentID}";
                        await _notificationSetter.ForCommentBeAgreedAsync(comment.Content, ownerProfile.ProfileID, url, toasts);
                    }
                }

                // Check achievements for user
                var userProfile = await _context.Profiles
                        .FirstOrDefaultAsync(p => p.ProfileID == currentUserID);

                userToasts = AchievementHelper.AgreeCountAsync(_context, userProfile.ProfileID).Result;
            }
            else
            {
                // Un-agree
                _context.Agrees.Remove(existingAgree);
            }

            await _context.SaveChangesAsync();

            var response = new
            {
                agreesCount = _context.Agrees.Count(a => a.CommentID == id),
                Toasts = userToasts
            };

            return Ok(response);
        }

        [HttpPost("{id}/block")]
        public async Task<IActionResult> ToggleBlockAsync(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            comment.Block = !comment.Block;
            _context.Entry(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

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
}