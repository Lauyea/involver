using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Models.ViewModels.Api;
using Involver.Services.NotificationSetterService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [Route("api/comments")]
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
        public async Task<IActionResult> GetComments([FromQuery] string from, [FromQuery] int fromID, [FromQuery] int page = 1, [FromQuery] string sortBy = "oldest")
        {
            IQueryable<Comment> commentsQuery = _context.Comments
                .Include(c => c.Profile)
                .Include(c => c.Agrees)
                .Include(c => c.Dices)
                .Include(c => c.Messages.OrderByDescending(m => m.UpdateTime).Take(Parameters.MessagePageSize))
                    .ThenInclude(m => m.Profile);

            bool isCommentOrderFixed = false;

            switch (from.ToLower())
            {
                case "article":
                    var article = await _context.Articles.FindAsync(fromID);
                    if (article != null)
                    {
                        isCommentOrderFixed = article.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery.Where(c => c.ArticleID == fromID);
                    break;
                case "novel":
                    var novel = await _context.Novels.FindAsync(fromID);
                    if (novel != null)
                    {
                        isCommentOrderFixed = novel.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery.Where(c => c.NovelID == fromID);
                    break;
                case "episode":
                    var episode = await _context.Episodes.FindAsync(fromID);
                    if (episode != null)
                    {
                        isCommentOrderFixed = episode.IsCommentOrderFixed;
                    }
                    commentsQuery = commentsQuery.Where(c => c.EpisodeID == fromID);
                    break;
                default:
                    return BadRequest("Invalid 'from' parameter.");
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

            var paginatedComments = await PaginatedList<Comment>.CreateAsync(commentsQuery, page, DataAccess.Common.Parameters.CommetPageSize);

            var currentUserID = _userManager.GetUserId(User);

            var commentDtos = new List<CommentDto>();

            foreach (var comment in paginatedComments)
            {
                var user = await _userManager.FindByIdAsync(comment.ProfileID);
                var canEdit = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update)).Succeeded;
                var canDelete = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Delete)).Succeeded;
                var canBlock = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded;

                commentDtos.Add(new CommentDto
                {
                    CommentID = comment.CommentID,
                    Content = comment.Content,
                    UpdateTime = Involver.Helpers.TimePeriodHelper.Get(comment.UpdateTime),
                    ProfileID = comment.ProfileID,
                    UserName = comment.Profile.UserName,
                    UserImageUrl = !string.IsNullOrEmpty(comment.Profile.ImageUrl)
                        ? comment.Profile.ImageUrl
                        : $"https://www.gravatar.com/avatar/{Involver.Extensions.StringExtensions.ToMd5(user.Email)}?d=retro",
                    Involves = null, // Logic to be added
                    Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? new List<object>(),
                    Messages = comment.Messages?.Select(m => new { m.Profile.UserName, m.Content }).ToList<object>() ?? new List<object>(),
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
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto createDto)
        { 
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Banned)
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
                case "article":
                    comment.ArticleID = createDto.FromID;
                    break;
                case "novel":
                    comment.NovelID = createDto.FromID;
                    break;
                case "episode":
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
            int textDiceCount = Involver.Helpers.DiceHelper.ReplaceRollDiceString(ref contentWithDiceRolls);
            comment.Content = contentWithDiceRolls;

            if (textDiceCount > 0)
            {
                hasDices = true;
                // Add a placeholder to indicate text-based roll, as in original logic
                if (!comment.Dices.Any(d => d.Sides == 0) && comment.Dices.Count == 0)
                {
                    comment.Dices.Add(new Dice { Sides = 0, Value = 0 });
                }

                comment.Content += ($"<p>此段文字共用了 {textDiceCount} 次文字擲骰指令。</p>");
            }

            _context.Comments.Add(comment);

            // Mission check
            var commenterProfile = await _context.Profiles.Include(p => p.Missions).FirstOrDefaultAsync(p => p.ProfileID == user.Id);
            if (commenterProfile != null && commenterProfile.Missions.LeaveComment != true)
            {
                commenterProfile.Missions.LeaveComment = true;
                commenterProfile.VirtualCoins += 5;
                // TODO: Add a way to notify user of mission completion
            }

            await _context.SaveChangesAsync();

            // Achievement check
            await Involver.Helpers.AchievementHelper.CommentCountAsync(_context, user.Id);
            if (hasDices)
            {
                await Involver.Helpers.AchievementHelper.RollDicesAsync(_context, user.Id);
            }

            // Map to DTO to return
            var newCommentDto = new CommentDto
            {
                CommentID = comment.CommentID,
                Content = comment.Content,
                UpdateTime = Involver.Helpers.TimePeriodHelper.Get(comment.UpdateTime),
                ProfileID = comment.ProfileID,
                UserName = commenterProfile.UserName,
                UserImageUrl = !string.IsNullOrEmpty(commenterProfile.ImageUrl)
                    ? commenterProfile.ImageUrl
                    : $"https://www.gravatar.com/avatar/{Involver.Extensions.StringExtensions.ToMd5(user.Email)}?d=retro",
                Involves = null, // To be implemented
                Dices = comment.Dices?.Select(d => new { d.Value, d.Sides }).ToList<object>() ?? new List<object>(),
                Messages = new List<object>(), // New comment has no messages
                AgreesCount = 0,
                IsAgreedByCurrentUser = false,
                CanEdit = true, // User can always edit their own new comment
                CanDelete = true, // User can always delete their own new comment
                CanBlock = (await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Block)).Succeeded,
                IsBlocked = comment.Block
            };

            return CreatedAtAction(nameof(GetComments), new { from = createDto.From, fromID = createDto.FromID }, newCommentDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto updateDto)
        {
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, comment, CommentOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            comment.Content = updateDto.Content;
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

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
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
        public async Task<IActionResult> ToggleAgree(int id)
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
                            ownerProfile.VirtualCoins += 5;
                        }
                        
                        await _context.SaveChangesAsync(); // Save mission changes first
                        
                        var toasts = await Involver.Helpers.AchievementHelper.GetAgreeCountAsync(_context, ownerProfile.ProfileID);

                        // Set notification
                        var from = GetCommentSource(comment);
                        var fromId = GetCommentSourceId(comment);
                        var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromId}#{comment.CommentID}";
                        await _notificationSetter.ForCommentBeAgreedAsync(comment.Content, ownerProfile.ProfileID, url, toasts);
                    }
                }
            }
            else
            {
                // Un-agree
                _context.Agrees.Remove(existingAgree);
            }

            await _context.SaveChangesAsync();

            return Ok(new { agreesCount = _context.Agrees.Count(a => a.CommentID == id) });
        }

        [HttpPost("{id}/block")]
        public async Task<IActionResult> ToggleBlock(int id)
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
            if (comment.ArticleID != null) return "Articles";
            if (comment.NovelID != null) return "Novels";
            if (comment.EpisodeID != null) return "Episodes";
            if (comment.AnnouncementID != null) return "Announcements";
            if (comment.FeedbackID != null) return "Feedbacks";
            return string.Empty;
        }

        private int? GetCommentSourceId(Comment comment)
        {
            if (comment.ArticleID != null) return comment.ArticleID;
            if (comment.NovelID != null) return comment.NovelID;
            if (comment.EpisodeID != null) return comment.EpisodeID;
            if (comment.AnnouncementID != null) return comment.AnnouncementID;
            if (comment.FeedbackID != null) return comment.FeedbackID;
            return null;
        }
    }
}
