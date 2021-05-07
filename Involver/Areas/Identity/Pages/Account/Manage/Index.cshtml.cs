using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Involver.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly SignInManager<InvolverUser> _signInManager;
        private ApplicationDbContext Context;

        public IndexModel(
            UserManager<InvolverUser> userManager,
            SignInManager<InvolverUser> signInManager, 
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            Context = context;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public InvolverUser user { get; set; }

        public Models.Profile Profile { get; set; }

        public class InputModel
        {
            [DataType(DataType.Text)]
            [Display(Name = "用戶名")]
            public string UserName { get; set; }

            [Phone]
            [Display(Name = "手機號碼")]
            public string PhoneNumber { get; set; }
            [Display(Name = "銀行帳號")]
            public string BankAccount { get; set; }
        }

        private async Task LoadAsync(InvolverUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var bankAccount = _userManager.GetUserAsync(User).Result.BankAccount;

            Input = new InputModel
            {
                UserName = userName,
                PhoneNumber = phoneNumber,
                BankAccount = bankAccount
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            Profile = await Context.Profiles.Where(p => p.ProfileID == user.Id).FirstOrDefaultAsync();

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            
            var userName = await _userManager.GetUserNameAsync(user);
            if (Input.UserName != userName && Input.UserName != null)
            {
                var SameNameProfile = await Context.Profiles.Where(p => p.UserName == Input.UserName).FirstOrDefaultAsync();
                Profile = await Context.Profiles.Where(p => p.ProfileID == user.Id).FirstOrDefaultAsync();
                if (SameNameProfile != null)
                {
                    ModelState.AddModelError("Input.UserName", "已有重複的用戶名");
                    return Page();
                }
                Profile.UserName = Input.UserName;
                Profile.CanChangeUserName = false;
                var setNameResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                Context.Attach(Profile).State = EntityState.Modified;
                if (!setNameResult.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting user's name for user with ID '{userId}'.");
                }
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting phone number for user with ID '{userId}'.");
                }
            }

            if (Input.BankAccount != user.BankAccount)
            {
                user.BankAccount = Input.BankAccount;
            }
            await _userManager.UpdateAsync(user);

            await Context.SaveChangesAsync();

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "你的個人資料已經更新";
            return RedirectToPage();
        }
    }
}
