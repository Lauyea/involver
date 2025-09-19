using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Novels
{
    [AllowAnonymous]
    public class TagsModel : DI_BasePageModel
    {
        public TagsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public List<NovelTag> NovelTags { get; set; }

        public async Task OnGetAsync()
        {
            NovelTags = await _context.NovelTags
                //.Include(t => t.Novels)
                .OrderByDescending(t => t.Novels.Count)
                .ToListAsync();
        }
    }
}