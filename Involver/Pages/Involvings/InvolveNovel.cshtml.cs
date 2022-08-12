using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings
{
    [AllowAnonymous]
    public class InvolveNovelModel : DI_BasePageModel
    {
        public InvolveNovelModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Involving Involving { get; set; }
        public Novel Novel { get; set; }
        public int NovelID { get; set; }
        public string UserID { get; set; }

        public Profile Involver { get; set; }

        private async Task LoadAsync(int id)
        {
            UserID = _userManager.GetUserId(User);
            Novel = await _context.Novels
                .Include(n => n.Profile)
                .Where(n => n.NovelID == id)
                .FirstOrDefaultAsync();
            Involver = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            NovelID = id;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            if (Novel != null)
            {
                return Page();
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostSingleInvolveAsync(int id)
        {
            NovelID = id;
            await LoadAsync(NovelID);

            if (UserID == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Novel == null)
            {
                return NotFound();
            }

            Profile Involver = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
            Profile Creator = await _context.Profiles
            .Where(p => p.ProfileID == Novel.ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < Involving.Value)
            {
                ModelState.AddModelError(Involving.Value.ToString(), "帳戶餘額不足");
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(Involving.Value * 0.5);//創作直接贊助，作者得50%分潤
            _context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= Involving.Value;
            Involver.UsedCoins += Involving.Value;
            _context.Attach(Involver).State = EntityState.Modified;

            Novel.MonthlyCoins += Involving.Value;
            Novel.TotalCoins += Involving.Value;
            _context.Attach(Novel).State = EntityState.Modified;

            Involving ExistingInvolving = await _context.Involvings
                .Where(i => i.NovelID == NovelID)
                .Where(i => i.InvolverID == UserID)
                .FirstOrDefaultAsync();

            if (ExistingInvolving != null)
            {
                ExistingInvolving.Value = Involving.Value;
                ExistingInvolving.MonthlyValue += Involving.Value;
                ExistingInvolving.TotalValue += Involving.Value;
                ExistingInvolving.LastTime = DateTime.Now;
                _context.Attach(ExistingInvolving).State = EntityState.Modified;
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
                    NovelID = NovelID
                };
                _context.Involvings.Add(newInvolving);
            }
            await _context.SaveChangesAsync();
            StatusMessage = "Involve成功，總共" + Involving.Value + " In幣，感謝以實體行動鼓勵創作";
            return Page();
        }

        public async Task<IActionResult> OnPostMonthlyInvolveAsync(int id)
        {
            NovelID = id;
            int InvolveValue = 150;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            Follow existingFollow = await _context.Follows
                .Where(f => f.NovelID == NovelID)
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefaultAsync();

            if (existingFollow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    NovelMonthlyInvolver = true,
                    UpdateTime = DateTime.Now,
                    NovelID = NovelID
                };
                _context.Follows.Add(newFollow);
            }
            else
            {
                if (existingFollow.NovelMonthlyInvolver)
                {
                    StatusMessage = "已經Involve過了，如要繼續Involve，可以使用直接Involve的功能";
                    return Page();
                }
                existingFollow.NovelMonthlyInvolver = true;
                existingFollow.UpdateTime = DateTime.Now;
                _context.Attach(existingFollow).State = EntityState.Modified;
            }

            Profile Creator = await _context.Profiles
            .Where(p => p.ProfileID == Novel.ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < InvolveValue)
            {
                StatusMessage = "Error: 帳戶餘額不足";
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(InvolveValue * 0.6);//創作每月Involve，作者得60%分潤
            _context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= InvolveValue;
            Involver.UsedCoins += InvolveValue;
            _context.Attach(Involver).State = EntityState.Modified;

            Novel.MonthlyCoins += InvolveValue;
            Novel.TotalCoins += InvolveValue;
            _context.Attach(Novel).State = EntityState.Modified;

            Involving ExistingInvolving = await _context.Involvings
                .Where(i => i.NovelID == NovelID)
                .Where(i => i.InvolverID == UserID)
                .FirstOrDefaultAsync();

            if (ExistingInvolving != null)
            {
                ExistingInvolving.Value = InvolveValue;
                ExistingInvolving.MonthlyValue += InvolveValue;
                ExistingInvolving.TotalValue += InvolveValue;
                ExistingInvolving.LastTime = DateTime.Now;
                _context.Attach(ExistingInvolving).State = EntityState.Modified;
            }
            else
            {
                Involving newInvolving = new Involving()
                {
                    Value = InvolveValue,
                    MonthlyValue = InvolveValue,
                    TotalValue = InvolveValue,
                    LastTime = DateTime.Now,
                    InvolverID = UserID,
                    NovelID = NovelID
                };
                _context.Involvings.Add(newInvolving);
            }

            await _context.SaveChangesAsync();

            StatusMessage = "每月Involve成功，感謝以實體行動鼓勵創作";
            return Page();
        }

        public async Task<IActionResult> OnPostUnInvolveAsync(int id)
        {
            NovelID = id;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            Follow follow = await _context.Follows
                .Where(f => f.NovelID == NovelID)
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefaultAsync();

            if(follow == null)
            {
                StatusMessage = "Error: 無效操作";
                return Page();
            }
            else
            {
                if(!follow.NovelMonthlyInvolver)
                {
                    StatusMessage = "Error: 您沒有每月Involve這部創作";
                    return Page();
                }
                follow.NovelMonthlyInvolver = false;
            }
            _context.Attach(follow).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            StatusMessage = "UnInvolve成功";
            return Page();
        }
    }
}
