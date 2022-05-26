using Involver.Authorization.Payment;
using Involver.Common;
using Involver.Data;
using Involver.Models.StatisticalData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

            Payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentID == id);

            if (Payment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
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

            Payment = await _context.Payments.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Payment,
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Payment != null)
            {
                _context.Payments.Remove(Payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
