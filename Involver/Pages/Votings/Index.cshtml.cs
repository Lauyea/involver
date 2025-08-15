using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

namespace Involver.Pages.Votings
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Voting> Voting { get;set; }

        public async Task OnGetAsync()
        {
            Voting = await _context.Votings
                .Include(v => v.Episode).ToListAsync();
        }
    }
}
