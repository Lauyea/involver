using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Involver.Data;
using Involver.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Feedback;
using Microsoft.EntityFrameworkCore;
using Involver.Models;
using Microsoft.Azure.KeyVault.Models;

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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await UserManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            Feedback.OwnerID = UserManager.GetUserId(User);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
                    var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.OwnerID);
                    emptyFeedback.OwnerID = Feedback.OwnerID;
                    emptyFeedback.OwnerName = tempUser.UserName;
                    Context.Feedbacks.Add(emptyFeedback);
                    await Context.SaveChangesAsync();

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
