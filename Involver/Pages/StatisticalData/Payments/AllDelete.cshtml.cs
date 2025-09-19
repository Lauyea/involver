using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Involver.Authorization.Payment;
using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

            Payments = await _context.Payments.ToListAsync();

            if (Payments.Count == 0)
            {
                return Content("µL¸ê®Æ");
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
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
            Payments = await _context.Payments.ToListAsync();

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Payments[0],
                                                 PaymentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Payments != null)
            {
                _context.Payments.RemoveRange(Payments);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}