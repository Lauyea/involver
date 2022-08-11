using Involver.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Common
{
    public class DI_BasePageModel : PageModel
    {
        protected ApplicationDbContext _context { get; }
        protected IAuthorizationService _authorizationService { get; }
        protected UserManager<InvolverUser> _userManager { get; }

        [TempData]
        public string ToastsJson { get; set; }

        public List<Toast> Toasts { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public DI_BasePageModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager) : base()
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }
    }
}
