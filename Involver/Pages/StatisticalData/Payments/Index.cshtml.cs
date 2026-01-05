using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Pages.StatisticalData.Payments;

public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public IList<Payment> Payments { get; set; }

    public IActionResult OnGet()
    {
        var isAuthorized = User.IsInRole(Authorization.Payment.Payments.PaymentManagersRole) ||
                       User.IsInRole(Authorization.Payment.Payments.PaymentAdministratorsRole);

        if (!isAuthorized)
        {
            return Forbid();
        }

        Payments = Context.Payments.OrderByDescending(p => p.PaymentDate).ToList();
        return Page();
    }
}