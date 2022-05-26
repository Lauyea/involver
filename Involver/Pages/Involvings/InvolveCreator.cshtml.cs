using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings
{
    [AllowAnonymous]
    public class InvolveCreatorModel : DI_BasePageModel
    {
        public InvolveCreatorModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public string StatusMessage { get; set; }

        [BindProperty]
        public Involving Involving { get; set; }
        public Profile Profile { get; set; }
        public string ProfileID { get; set; }
        public string UserID { get; set; }
        public Profile Involver { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Involver = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            ProfileID = id;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            if (Profile != null)
            {
                return Page();
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostSingleInvolveAsync(string id)
        {
            ProfileID = id;
            await LoadAsync(ProfileID);

            if (UserID == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Profile == null)
            {
                return NotFound();
            }

            Profile Involver = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .FirstOrDefaultAsync();
            Profile Creator = Profile;

            if (Involver.RealCoins < Involving.Value)
            {
                ModelState.AddModelError(Involving.Value.ToString(), "�b��l�B����");
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(Involving.Value * 0.5);//�Ч@�̪����٧U�A�@�̱o50%����
            _context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= Involving.Value;
            _context.Attach(Involver).State = EntityState.Modified;

            Involving ExistingInvolving = await _context.Involvings
                .Where(i => i.ProfileID == ProfileID)
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
                    ProfileID = ProfileID
                };
                _context.Involvings.Add(newInvolving);
            }
            await _context.SaveChangesAsync();
            StatusMessage = "Involve���\�A�`�@" + Involving.Value + " In���A�P�¥H�����ʹ��y�Ч@";
            return Page();
        }

        public async Task<IActionResult> OnPostMonthlyInvolveAsync(string id)
        {
            ProfileID = id;
            int InvolveValue = 150;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            Follow existingFollow = await _context.Follows
                .Where(f => f.ProfileID == ProfileID)
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefaultAsync();

            if (existingFollow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    NovelMonthlyInvolver = true,
                    UpdateTime = DateTime.Now,
                    ProfileID = ProfileID
                };
                _context.Follows.Add(newFollow);
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
                _context.Attach(existingFollow).State = EntityState.Modified;
            }

            Profile Creator = await _context.Profiles
            .Where(p => p.ProfileID == ProfileID)
            .FirstOrDefaultAsync();

            if (Involver.RealCoins < InvolveValue)
            {
                StatusMessage = "Error: �b��l�B����";
                return Page();
            }
            Creator.MonthlyCoins += (decimal)(InvolveValue * 0.6);//�Ч@�̨C��Involve�A�@�̱o60%����
            _context.Attach(Creator).State = EntityState.Modified;
            Involver.RealCoins -= InvolveValue;
            _context.Attach(Involver).State = EntityState.Modified;

            Involving ExistingInvolving = await _context.Involvings
                .Where(i => i.ProfileID == ProfileID)
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
                    ProfileID = ProfileID
                };
                _context.Involvings.Add(newInvolving);
            }

            await _context.SaveChangesAsync();

            StatusMessage = "�C��Involve���\�A�P�¥H�����ʹ��y�Ч@";
            return Page();
        }

        public async Task<IActionResult> OnPostUnInvolveAsync(string id)
        {
            ProfileID = id;
            await LoadAsync(id);
            if (UserID == null)
            {
                return Challenge();
            }

            Follow follow = await _context.Follows
                .Where(f => f.ProfileID == ProfileID)
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefaultAsync();

            if (follow == null)
            {
                StatusMessage = "Error: �L�ľާ@";
                return Page();
            }
            else
            {
                if (!follow.NovelMonthlyInvolver)
                {
                    StatusMessage = "Error: �z�S���C��Involve�o���Ч@";
                    return Page();
                }
                follow.NovelMonthlyInvolver = false;
            }
            _context.Attach(follow).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            StatusMessage = "UnInvolve���\";
            return Page();
        }
    }
}
