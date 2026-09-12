using System;
using System.Threading.Tasks;
using DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Preview
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Token { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public bool IsRevoked { get; set; }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToPage("/Functions/Preview");
            }

            var preview = await _context.Previews.AsNoTracking().FirstOrDefaultAsync(p => p.Token == token);
            if (preview == null)
            {
                return NotFound();
            }

            Token = token;
            IsExpired = preview.ExpiresAt <= DateTime.UtcNow;
            IsRevoked = preview.RevokedAt != null;

            return Page();
        }
    }
}
