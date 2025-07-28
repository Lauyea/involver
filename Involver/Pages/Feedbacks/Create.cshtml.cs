using Involver.Authorization.Feedback;
using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks
{
    public class CreateModel : DI_BasePageModel
    {

        public CreateModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Feedback Feedback { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if(Feedback.Content?.Length > Parameters.ArticleLength)
            {
                Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            Feedback.OwnerID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Feedback,
                                                        FeedbackOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Feedback emptyFeedback = 
                new Feedback {
                    Title = "temp title",
                    Content = "temp content",
                Comments = new List<Comment> 
                {
                    //防止Comment找不到所屬的Feedback
                    new Comment
                    {
                        ProfileID = Feedback.OwnerID,
                        FeedbackID = Feedback.FeedbackID,
                        Block = true,
                        Content = "anchor",
                        //加了Profile的話TryUpdateModelAsync就不動了
                        //Profile = new Profile
                        //{
                        //    ProfileID = "0",
                        //    UserName = "",
                        //    Views = 0
                        //}
                    }
                } 
                };
            try
            {
                //Protect from overposting attacks
                if (await TryUpdateModelAsync<Feedback>(
                    emptyFeedback,
                    "feedback",   // Prefix for form value.
                    f => f.Title, f => f.Content))
                {
                    emptyFeedback.UpdateTime = DateTime.Now;
                    var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.OwnerID);
                    emptyFeedback.OwnerID = Feedback.OwnerID;
                    emptyFeedback.OwnerName = tempUser.UserName;
                    _context.Feedbacks.Add(emptyFeedback);
                    await _context.SaveChangesAsync();

                    var toasts = await Helpers.AchievementHelper.FeedbackCountAsync(_context, Feedback.OwnerID);

                    Toasts.AddRange(toasts);

                    ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                string errormessage = ex.ToString();
            }

            return Page();
        }
    }
}
