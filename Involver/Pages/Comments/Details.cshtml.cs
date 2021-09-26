using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Message;

namespace Involver.Pages.Comments
{
    [AllowAnonymous]
    public class DetailModel : DI_BasePageModel
    {
        public DetailModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public Comment Comment { get; set; }

        public Comment PreviousComment { get; set; }
        public Comment NextComment { get; set; }
        public PaginatedList<Message> Messages { get; set; }

        [BindProperty]
        public Message Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Comment = await Context.Comments
                .Include(c => c.Announcement)
                .Include(c => c.Article)
                .Include(c => c.Episode)
                .Include(c => c.Feedback)
                .Include(c => c.Novel)
                .Include(c => c.Dices)
                .Include(c => c.Profile).FirstOrDefaultAsync(m => m.CommentID == id);

            if (Comment.Announcement != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Article != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.ArticleID == Comment.Article.ArticleID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.ArticleID == Comment.Article.ArticleID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Episode != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Feedback != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Novel != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.NovelID == Comment.Novel.NovelID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.NovelID == Comment.Novel.NovelID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment == null)
            {
                return NotFound();
            }

            await SetMessages(id, pageIndex);

            return Page();
        }

        private async Task SetMessages(int? id, int? pageIndex)
        {
            IQueryable<Message> messages = from m in Context.Messages
                                           select m;
            messages = messages
                .Include(m => m.Comment)
                    .ThenInclude(c => c.Episode)
                        .ThenInclude(e => e.Novel)
                            .ThenInclude(n => n.Involvers)
                .Include(m => m.Agrees)
                .Include(m => m.Profile)
                .Where(m => m.CommentID == id)
                .OrderBy(m => m.CommentID);

            int pageSize = 100;
            Messages = await PaginatedList<Message>.CreateAsync(
                messages, pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostCreateMessageAsync(int id, int pageIndex)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await UserManager.GetUserAsync(User);
            if(user == null)
            {
                return Challenge();
            }
            if (user.Banned)
            {
                return Forbid();
            }

            Message.ProfileID = UserManager.GetUserId(User);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, Message,
                                        MessageOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Message.CommentID = id;
            Message.UpdateTime = DateTime.Now;

            Context.Messages.Add(Message);
            await Context.SaveChangesAsync();

            return RedirectToPage("/Comments/Details", "OnGet", new { id, pageIndex });
        }

        public async Task<IActionResult> OnPostAgreeAsync(int id, int fromID, int pageIndex)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.MessageID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    MessageID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);
                await CheckMissionBeAgreed(id);

                await Context.SaveChangesAsync();
            }
            else
            {
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", "OnGet", new { id = fromID, pageIndex }, id.ToString());
        }

        public async Task<IActionResult> OnPostAgreeMessageAsync(int id)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.MessageID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Message message = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    MessageID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                message = await Context.Messages
                    .Where(c => c.MessageID == id)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                string UserID = message.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                message = await Context.Messages
                    .Where(c => c.MessageID == id)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            if (message != null)
            {
                return Content(message.Agrees.Count().ToString());
            }
            else
            {
                return BadRequest();
            }
        }

        private async Task CheckMissionBeAgreed(int id)
        {
            //Check mission:BeAgreed //CheckMissionBeAgreed
            Comment comment = await Context.Comments
                .Where(c => c.CommentID == id)
                .Include(c => c.Profile)
                .FirstOrDefaultAsync();
            string UserID = comment.ProfileID;
            Profile Commenter = await Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (Commenter.Missions.BeAgreed != true)
            {
                Commenter.Missions.BeAgreed = true;
                Commenter.VirtualCoins += 5;
                Context.Attach(Commenter).State = EntityState.Modified;
            }
            //Check other missions
            Missions missions = Commenter.Missions;
            if (missions.WatchArticle
                && missions.Vote
                && missions.LeaveComment
                && missions.ViewAnnouncement
                && missions.ShareCreation
                && missions.BeAgreed)
            {
                Commenter.Missions.CompleteOtherMissions = true;
                Context.Attach(Commenter).State = EntityState.Modified;
            }
        }
    }
}
