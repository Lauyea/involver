using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models.StatisticalData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.StatisticalData.Payments
{
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

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
}
