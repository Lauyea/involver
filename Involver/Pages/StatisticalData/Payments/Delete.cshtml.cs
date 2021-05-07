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
    public class DeleteModel : DI_BasePageModel
    {
        public DeleteModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        [BindProperty]
        public Payment Payment { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Payment = await Context.Payments.FirstOrDefaultAsync(p => p.PaymentID == id);

            if (Payment == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Payment,
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Payment = await Context.Payments.FindAsync(id);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Payment,
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Payment != null)
            {
                Context.Payments.Remove(Payment);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
