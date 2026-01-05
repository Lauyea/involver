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

public class AllDeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public IList<Payment> Payments { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {

        Payments = await Context.Payments.ToListAsync();

        if (Payments.Count == 0)
        {
            return Content("無資料");
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