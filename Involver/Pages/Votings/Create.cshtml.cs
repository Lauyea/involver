using Involver.Authorization.Voting;
using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Votings
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Voting voting = _context.Votings.Where(v => v.EpisodeID == id).FirstOrDefault();

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, voting,
                                                        VotingOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == _userManager.GetUserId(User))
                .FirstOrDefaultAsync();

            return Page();
        }

        [BindProperty]
        public Voting Voting { get; set; }
        [BindProperty]
        public NormalOption NormalOption1 { get; set; }
        [BindProperty]
        public NormalOption NormalOption2 { get; set; }
        [BindProperty]
        public NormalOption NormalOption3 { get; set; }
        public Profile Profile { get; set; }
        public string ErrorMessage { get; set; }

        public List<SelectListItem> Policys { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = Voting.PolicyType.Equality.ToString(), Text = "平等" },
            new SelectListItem { Value = Voting.PolicyType.Liberty.ToString(), Text = "自由" }
        };

        public List<SelectListItem> Limits { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = Voting.LimitType.Time.ToString(), Text = "限時" },
            new SelectListItem { Value = Voting.LimitType.Number.ToString(), Text = "限量" },
            new SelectListItem { Value = Voting.LimitType.Value.ToString(), Text = "限值" }
        };

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == _userManager.GetUserId(User))
                .FirstOrDefaultAsync();

            if (Voting.Limit == Voting.LimitType.Time && Voting.DeadLine == null)
            {
                ErrorMessage = "限時投票必須要設定期限";
                return Page();
            }
            else if(Voting.Limit == Voting.LimitType.Number && Voting.NumberLimit == null)
            {
                ErrorMessage = "限量投票必須要設定人數上限";
                return Page();
            }
            else if (Voting.Limit == Voting.LimitType.Value && Voting.CoinLimit == null)
            {
                ErrorMessage = "限值投票必須要設定總InCoins上限";
                return Page();
            }

            Voting voting = _context.Votings.Where(v => v.EpisodeID == id).FirstOrDefault();

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, voting,
                                                        VotingOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (NormalOption1.Content == null || NormalOption2.Content == null || NormalOption3.Content == null)
            {
                ErrorMessage = "選項內容不可為空白";
                return Page();
            }

            NormalOption1.OwnerID = _userManager.GetUserId(User);
            NormalOption1.TotalCoins = 0;
            NormalOption2.OwnerID = _userManager.GetUserId(User);
            NormalOption2.TotalCoins = 0;
            NormalOption3.OwnerID = _userManager.GetUserId(User);
            NormalOption3.TotalCoins = 0;

            Voting NewVoting = new Voting()
            {
                NormalOptions = new List<NormalOption>
                {
                    NormalOption1,
                    NormalOption2,
                    NormalOption3
                }
            };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Voting>(
                NewVoting,
                "Voting",   // Prefix for form value.
                v => v.Policy, v => v.Limit, v => v.Threshold, v => v.Title))
            {
                NewVoting.OwnerID = _userManager.GetUserId(User);
                NewVoting.Mode = Voting.ModeType.Normal;
                NewVoting.End = false;
                NewVoting.TotalCoins = 0;
                NewVoting.TotalNumber = 0;
                NewVoting.CreateTime = DateTime.Now;
                NewVoting.EpisodeID = id;
                if(NewVoting.Limit == Voting.LimitType.Time)
                {
                    NewVoting.DeadLine = Voting.DeadLine;
                }
                else if(NewVoting.Limit == Voting.LimitType.Number)
                {
                    NewVoting.NumberLimit = Voting.NumberLimit;
                }
                else if(NewVoting.Limit == Voting.LimitType.Value)
                {
                    NewVoting.CoinLimit = Voting.CoinLimit;
                }

                _context.Votings.Add(NewVoting);
                await _context.SaveChangesAsync();

                return RedirectToPage("/Episodes/Details", "OnGet", new { id }, "Voting");
            }
            return Page();
        }
    }
}
