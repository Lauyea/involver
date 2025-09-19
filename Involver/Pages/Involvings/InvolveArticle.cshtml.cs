using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings
{
    [AllowAnonymous]
    public class InvolveArticleModel : DI_BasePageModel
    {
        public InvolveArticleModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        [BindProperty]
        public Involving Involving { get; set; }
        public Article Article { get; set; }
        public int ArticleID { get; set; }
        public string UserID { get; set; }

        private async Task LoadAsync(int id)
        {
            UserID = _userManager.GetUserId(User);
            Article = await _context.Articles
                .Include(a => a.Profile)
                .Where(a => a.ArticleID == id)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ArticleID = id;
            await LoadAsync(id);

            if (UserID == null)
            {
                return Challenge();
            }

            if (Article != null)
            {
                return Page();
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            ArticleID = id;
            await LoadAsync(ArticleID);

            if (UserID == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Article == null)
            {
                return NotFound();
            }

            Profile Involver = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
            Profile Creator = await _context.Profiles
            .Where(p => p.ProfileID == Article.ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < Involving.Value)
            {
                ModelState.AddModelError(Involving.Value.ToString(), "帳戶餘額不足");
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(Involving.Value * 0.5);//文章直接贊助，作者得50%分潤
            Involver.RealCoins -= Involving.Value;
            Involver.UsedCoins += Involving.Value;

            Article.MonthlyCoins += Involving.Value;
            Article.TotalCoins += Involving.Value;

            Involving ExistingInvolving = await _context.Involvings
                .Where(i => i.ArticleID == ArticleID)
                .Where(i => i.InvolverID == UserID)
                .FirstOrDefaultAsync();

            if (ExistingInvolving != null)
            {
                ExistingInvolving.Value = Involving.Value;
                ExistingInvolving.MonthlyValue += Involving.Value;
                ExistingInvolving.TotalValue += Involving.Value;
                ExistingInvolving.LastTime = DateTime.Now;
            }
            else
            {
                Involving newInvolving = new Involving()
                {
                    Value = Involving.Value,
                    MonthlyValue = Involving.Value,
                    TotalValue = Involving.Value,
                    LastTime = DateTime.Now,
                    InvolverID = UserID,
                    ArticleID = ArticleID
                };
                _context.Involvings.Add(newInvolving);
            }
            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.UseCoinsCountAsync(_context, Involver.ProfileID, Involver.UsedCoins);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("/Articles/Details", "OnGet", new { id = Article.ArticleID });
        }
    }
}