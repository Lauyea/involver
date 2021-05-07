using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Operations;
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

        //[TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public Involving Involving { get; set; }
        public Novel Novel { get; set; }
        public int NovelID { get; set; }
        public string UserID { get; set; }

        public Profile Involver { get; set; }

        private async Task LoadAsync(int id)
        {
            UserID = UserManager.GetUserId(User);
            Novel = await Context.Novels
                .Include(n => n.Profile)
                .Where(n => n.NovelID == id)
                .FirstOrDefaultAsync();
            Involver = await Context.Profiles
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

            Profile Involver = await Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
            Profile Creator = await Context.Profiles
            .Where(p => p.ProfileID == Novel.ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < Involving.Value)
            {
                ModelState.AddModelError(Involving.Value.ToString(), "�b��l�B����");
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(Involving.Value * 0.5);//�Ч@�����٧U�A�@�̱o50%����
            Context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= Involving.Value;
            Context.Attach(Involver).State = EntityState.Modified;

            Novel.MonthlyCoins += Involving.Value;
            Novel.TotalCoins += Involving.Value;
            Context.Attach(Novel).State = EntityState.Modified;

            Involving ExistingInvolving = await Context.Involvings
                .Where(i => i.NovelID == NovelID)
                .Where(i => i.InvolverID == UserID)
                .FirstOrDefaultAsync();

            if (ExistingInvolving != null)
            {
                ExistingInvolving.Value = Involving.Value;
                ExistingInvolving.MonthlyValue += Involving.Value;
                ExistingInvolving.TotalValue += Involving.Value;
                ExistingInvolving.LastTime = DateTime.Now;
                Context.Attach(ExistingInvolving).State = EntityState.Modified;
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
                Context.Involvings.Add(newInvolving);
            }
            await Context.SaveChangesAsync();
            StatusMessage = "Involve���\�A�`�@" + Involving.Value + " In���A�P�¥H�����ʹ��y�Ч@";
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

            Follow existingFollow = await Context.Follows
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
                Context.Follows.Add(newFollow);
            }
            else
            {
                if (existingFollow.NovelMonthlyInvolver)
                {
                    StatusMessage = "�w�gInvolve�L�F�A�p�n�~��Involve�A�i�H�ϥΪ���Involve���\��";
                    return Page();
                }
                existingFollow.NovelMonthlyInvolver = true;
                existingFollow.UpdateTime = DateTime.Now;
                Context.Attach(existingFollow).State = EntityState.Modified;
            }

            Profile Creator = await Context.Profiles
            .Where(p => p.ProfileID == Novel.ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < InvolveValue)
            {
                StatusMessage = "Error: �b��l�B����";
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(InvolveValue * 0.6);//�Ч@�C��Involve�A�@�̱o60%����
            Context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= InvolveValue;
            Context.Attach(Involver).State = EntityState.Modified;

            Novel.MonthlyCoins += InvolveValue;
            Novel.TotalCoins += InvolveValue;
            Context.Attach(Novel).State = EntityState.Modified;

            Involving ExistingInvolving = await Context.Involvings
                .Where(i => i.NovelID == NovelID)
                .Where(i => i.InvolverID == UserID)
                .FirstOrDefaultAsync();

            if (ExistingInvolving != null)
            {
                ExistingInvolving.Value = InvolveValue;
                ExistingInvolving.MonthlyValue += InvolveValue;
                ExistingInvolving.TotalValue += InvolveValue;
                ExistingInvolving.LastTime = DateTime.Now;
                Context.Attach(ExistingInvolving).State = EntityState.Modified;
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
                Context.Involvings.Add(newInvolving);
            }

            await Context.SaveChangesAsync();

            StatusMessage = "�C��Involve���\�A�P�¥H�����ʹ��y�Ч@";
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

            Follow follow = await Context.Follows
                .Where(f => f.NovelID == NovelID)
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefaultAsync();

            if(follow == null)
            {
                StatusMessage = "Error: �L�ľާ@";
                return Page();
            }
            else
            {
                if(!follow.NovelMonthlyInvolver)
                {
                    StatusMessage = "Error: �z�S���C��Involve�o���Ч@";
                    return Page();
                }
                follow.NovelMonthlyInvolver = false;
            }
            Context.Attach(follow).State = EntityState.Modified;
            await Context.SaveChangesAsync();
            StatusMessage = "UnInvolve���\";
            return Page();
        }
    }
}
