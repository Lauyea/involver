using DataAccess.Data;
using DataAccess.Models;

using Involver.Authorization.Message;
using Involver.Common;
using Involver.Extensions; // Add this using for .ToMd5()
using Involver.Helpers; // Add this using for TimePeriodHelper
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers;

[Route("api/v1/messages")]
[ApiController]
public class MessagesApiController(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    INotificationSetter notificationSetter,
    IAchievementService achievementService,
    IMissionService missionService) : ControllerBase
{

    // GET: api/MessagesApi/ByComment/5
    [HttpGet("ByComment/{commentId}", Name = "GetMessages")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(int commentId)
    {
        var messages = await context.Messages
            .Include(m => m.Profile)
            .Include(m => m.Agrees)
            .Where(m => m.CommentID == commentId)
            .OrderBy(m => m.MessageID)
            .ToListAsync(); // Not AsNoTracking because we need to pass entities to authorization service

        var userIds = messages.Select(m => m.ProfileID).Distinct().ToList();
        var users = await context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u);
        var currentUserId = userManager.GetUserId(User);

        var result = new List<object>();

        foreach (var message in messages)
        {
            var canUpdate = (await authorizationService.AuthorizeAsync(User, message, MessageOperations.Update)).Succeeded;
            var canDelete = (await authorizationService.AuthorizeAsync(User, message, MessageOperations.Delete)).Succeeded;

            result.Add(new
            {
                message.MessageID,
                message.ProfileID,
                Content = CustomHtmlSanitizer.SanitizeHtml(message.Content),
                message.UpdateTime,
                UpdateTimeRelative = TimePeriodHelper.Get(message.UpdateTime),
                Profile = new
                {
                    message.Profile.UserName,
                    message.Profile.ImageUrl,
                    EmailMd5 = users.TryGetValue(message.ProfileID, out InvolverUser value) ? value.Email.ToMd5() : string.Empty
                },
                Agrees = message.Agrees.Select(a => new { a.AgreeID, a.ProfileID }).ToList(),
                IsAgreedByCurrentUser = currentUserId != null && message.Agrees.Any(a => a.ProfileID == currentUserId),
                CanUpdate = canUpdate,
                CanDelete = canDelete
            });
        }

        return result;
    }

    public class MessageCreateDto
    {
        public int CommentId { get; set; }
        public string Content { get; set; }
    }

    // POST: api/MessagesApi
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<object>> CreateMessage(MessageCreateDto messageDto)
    {
        if (string.IsNullOrEmpty(messageDto.Content))
        {
            return BadRequest("Content cannot be empty.");
        }

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }
        if (await context.IsBannedAsync(user.Id))
        {
            return Forbid();
        }

        var message = new Message
        {
            ProfileID = userManager.GetUserId(User),
            CommentID = messageDto.CommentId,
            Content = CustomHtmlSanitizer.SanitizeHtml(messageDto.Content),
            UpdateTime = DateTime.Now
        };

        var isAuthorized = await authorizationService.AuthorizeAsync(User, message, MessageOperations.Create);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Set notification
        var url = $"{Request.Scheme}://{Request.Host}/Comments/Details?id={message.CommentID}";
        await notificationSetter.ForMessageAsync(message.CommentID, message.ProfileID, message.Content, url);

        // Reload the message to include related entities for the response
        var createdMessage = await context.Messages
            .Include(m => m.Profile)
            .Include(m => m.Agrees)
            .FirstOrDefaultAsync(m => m.MessageID == message.MessageID);

        var result = new
        {
            createdMessage.MessageID,
            createdMessage.ProfileID,
            createdMessage.Content,
            createdMessage.UpdateTime,
            UpdateTimeRelative = TimePeriodHelper.Get(createdMessage.UpdateTime),
            Profile = new
            {
                createdMessage.Profile.UserName,
                createdMessage.Profile.ImageUrl,
                EmailMd5 = user.Email.ToMd5()
            },
            Agrees = createdMessage.Agrees.Select(a => new { a.AgreeID, a.ProfileID }).ToList(),
            IsAgreedByCurrentUser = false, // A newly created message won't be agreed by the creator yet
            CanUpdate = true, // Creator can always update
            CanDelete = true // Creator can always delete
        };

        return CreatedAtRoute(nameof(GetMessages), new { commentId = createdMessage.CommentID }, result);
    }

    // POST: api/MessagesApi/5/agree
    [HttpPost("{id}/agree")]
    [Authorize]
    public async Task<IActionResult> Agree(int id)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var message = await context.Messages
            .Include(m => m.Profile) // Needed for mission check
            .Include(m => m.Comment) // Needed for notification URL
            .FirstOrDefaultAsync(m => m.MessageID == id);

        if (message == null)
        {
            return NotFound();
        }

        // 只查自己那一筆，不載入這則回覆的全部 Agree
        var existingAgree = await context.Agrees
            .FirstOrDefaultAsync(a => a.MessageID == id && a.ProfileID == userId);

        if (existingAgree != null)
        {
            // User has agreed, so disagree
            context.Agrees.Remove(existingAgree);
            await context.SaveChangesAsync();

            return Ok(new { agreesCount = await CountAgreesAsync(id) });
        }

        // User has not agreed, so agree
        var agree = new Agree
        {
            MessageID = id,
            ProfileID = userId,
            UpdateTime = DateTime.Now
        };
        context.Agrees.Add(agree);

        try
        {
            await context.SaveChangesAsync(); // Save the agree first to get accurate count
        }
        catch (DbUpdateException)
        {
            // 連點時另一個請求已經先按讚，Unique 索引擋下重複的那筆，結果仍是「已按讚」
            context.Entry(agree).State = EntityState.Detached;
            return Ok(new { agreesCount = await CountAgreesAsync(id) });
        }

        // Check missions only when a new agree is added
        await CheckMissionsAsync(message, userId);
        // The second SaveChanges is inside CheckMissionsAsync

        // Check achievements for user
        List<Toast> userToasts = await achievementService.AgreeCountAsync(userId);

        return Ok(new { agreesCount = await CountAgreesAsync(id), Toasts = userToasts });
    }

    /// <summary>
    /// 計算回覆的按讚數
    /// </summary>
    /// <param name="messageId">MessageID</param>
    private Task<int> CountAgreesAsync(int messageId)
    {
        return context.Agrees.CountAsync(a => a.MessageID == messageId);
    }

    private async Task CheckMissionsAsync(Message message, string agreerId)
    {
        string authorId = message.ProfileID;
        // Do not check missions if the user agrees their own message
        if (authorId == agreerId)
        {
            return;
        }

        Profile authorProfile = await context.Profiles
            .Where(p => p.ProfileID == authorId)
            .FirstOrDefaultAsync();

        if (authorProfile == null) return;

        // 任務旗標與獎勵由任務服務原子更新，完成所有任務時會自動加獎勵幣
        await missionService.CompleteAsync(authorProfile.ProfileID, MissionType.BeAgreed);

        var toasts = await achievementService.GetAgreeCountAsync(authorProfile.ProfileID);

        // Set notification
        var url = $"{Request.Scheme}://{Request.Host}/Episodes/Details?id={message.Comment.EpisodeID}#comment-{message.CommentID}";
        await notificationSetter.ForMessageBeAgreedAsync(message.Content, authorProfile.ProfileID, url, toasts);

        await context.SaveChangesAsync();
    }

    // DELETE: api/MessagesApi/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await context.Messages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        var isAuthorized = await authorizationService.AuthorizeAsync(User, message, MessageOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        // Agrees 對回覆的 FK 是 NO ACTION，要先刪掉按讚，否則有人按過讚的回覆會刪除失敗
        await using var transaction = await context.Database.BeginTransactionAsync();

        await context.Agrees
            .Where(a => a.MessageID == id)
            .ExecuteDeleteAsync();

        context.Messages.Remove(message);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    public class MessageUpdateDto
    {
        public string Content { get; set; }
    }

    // PUT: api/MessagesApi/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> UpdateMessage(int id, MessageUpdateDto messageUpdateDto)
    {
        var message = await context.Messages
            .Include(m => m.Profile)
            .Include(m => m.Agrees)
            .FirstOrDefaultAsync(m => m.MessageID == id);

        if (message == null)
        {
            return NotFound();
        }

        var isAuthorized = await authorizationService.AuthorizeAsync(User, message, MessageOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(messageUpdateDto.Content))
        {
            return BadRequest("Content cannot be empty.");
        }

        message.Content = CustomHtmlSanitizer.SanitizeHtml(messageUpdateDto.Content);
        message.UpdateTime = DateTime.Now;

        await context.SaveChangesAsync();

        // Refetch the user to get the email for the MD5 hash
        var user = await userManager.FindByIdAsync(message.ProfileID);
        if (user == null) { return NotFound(); } // Should not happen

        var result = new
        {
            message.MessageID,
            message.ProfileID,
            message.Content,
            message.UpdateTime,
            UpdateTimeRelative = TimePeriodHelper.Get(message.UpdateTime),
            Profile = new
            {
                message.Profile.UserName,
                message.Profile.ImageUrl,
                EmailMd5 = user.Email.ToMd5()
            },
            Agrees = message.Agrees.Select(a => new { a.AgreeID, a.ProfileID }).ToList(),
            IsAgreedByCurrentUser = userManager.GetUserId(User) != null && message.Agrees.Any(a => a.ProfileID == userManager.GetUserId(User)),
            CanUpdate = true, // If we got here, it's true
            CanDelete = (await authorizationService.AuthorizeAsync(User, message, MessageOperations.Delete)).Succeeded
        };

        return result;
    }

}