using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models;

namespace Involver.Pages.Messages
{
    public class IndexModel : PageModel
    {
        private readonly Involver.Data.ApplicationDbContext _context;

        public IndexModel(Involver.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Message> Message { get;set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var isAuthorized = User.IsInRole(Authorization.Comment.Comments.CommentManagersRole) ||
                           User.IsInRole(Authorization.Comment.Comments.CommentAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }

            Message = await _context.Messages
                .Include(m => m.Comment).ToListAsync();

            return Page();
        }
    }
}
