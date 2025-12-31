using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Involver.Authorization.Payment;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData.Payments;

public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
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
}