using DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CommentsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<object>>> GetComments(int id)
        {
            var comments = await _context.Comments
                .Where(c => c.EpisodeID == id)
                .Include(c => c.Profile)
                .OrderBy(c => c.UpdateTime)
                .Select(c => new
                {
                    c.CommentID,
                    c.Content,
                    c.UpdateTime,
                    c.ProfileID,
                    Profile = new
                    {
                        c.Profile.UserName,
                        c.Profile.ImageUrl
                    }
                })
                .ToListAsync();

            return comments;
        }
    }
}
