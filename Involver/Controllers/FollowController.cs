using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    [ApiController]
    public class FollowController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;

        public FollowController(ApplicationDbContext context, UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task FollowAuthor(string id)
        {
            Profile profile = await _context.Profiles
                .Include(p => p.Follows)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                throw new NullReferenceException();
            }
            string UserID = _userManager.GetUserId(User);
            Follow follow = profile.Follows.Where(f => f.FollowerID == UserID).FirstOrDefault();

            if (follow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    ProfileID = id,
                    UpdateTime = DateTime.Now,
                    NovelMonthlyInvolver = false,
                    ProfileMonthlyInvolver = false
                };
                _context.Follows.Add(newFollow);
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task FollowNovel(int id)
        {
            Novel novel = await _context.Novels
                .Include(n => n.Follows)
                .Where(n => n.NovelID == id)
                .FirstOrDefaultAsync();

            if (novel == null)
            {
                throw new NullReferenceException();
            }
            string UserID = _userManager.GetUserId(User);
            Follow follow = novel.Follows.Where(f => f.FollowerID == UserID).FirstOrDefault();

            if (follow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    NovelID = id,
                    UpdateTime = DateTime.Now,
                    NovelMonthlyInvolver = false,
                    ProfileMonthlyInvolver = false
                };
                _context.Follows.Add(newFollow);
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }
    }
}
