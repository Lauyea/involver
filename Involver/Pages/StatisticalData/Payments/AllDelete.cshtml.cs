using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Authorization.Payment;
using Involver.Data;
using Involver.Models.StatisticalData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData.Payments
{
    public class AllDeleteModel : DI_BasePageModel
    {
        public AllDeleteModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        public IList<Payment> Payments { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {

            Payments = await Context.Payments.ToListAsync();

            if (Payments.Count == 0)
            {
                return Content("µL¸ê®Æ");
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Payments[0],
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Payments = await Context.Payments.ToListAsync();

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Payments[0],
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Payments != null)
            {
                Context.Payments.RemoveRange(Payments);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
