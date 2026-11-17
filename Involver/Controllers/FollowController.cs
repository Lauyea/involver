using DataAccess.Data;
using DataAccess.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    /// <summary>
    /// 追蹤／取消追蹤作者與小說
    /// </summary>
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

        /// <summary>
        /// 切換是否追蹤作者
        /// </summary>
        /// <param name="id">作者 ProfileID</param>
        /// <param name="cancellationToken">取消權杖</param>
        [HttpGet]
        public async Task<IActionResult> FollowAuthor(string id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            if (!await _context.Profiles.AnyAsync(p => p.ProfileID == id, cancellationToken))
            {
                return NotFound();
            }

            var follow = await _context.Follows
                .Where(f => f.ProfileID == id && f.FollowerID == userId)
                .FirstOrDefaultAsync(cancellationToken);

            return await ToggleFollowAsync(follow, () => new Follow
            {
                FollowerID = userId,
                ProfileID = id,
                UpdateTime = DateTime.Now,
                NovelMonthlyInvolver = false,
                ProfileMonthlyInvolver = false
            }, cancellationToken);
        }

        /// <summary>
        /// 切換是否追蹤小說
        /// </summary>
        /// <param name="id">NovelID</param>
        /// <param name="cancellationToken">取消權杖</param>
        [HttpGet]
        public async Task<IActionResult> FollowNovel(int id, CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            if (!await _context.Novels.AnyAsync(n => n.NovelID == id, cancellationToken))
            {
                return NotFound();
            }

            var follow = await _context.Follows
                .Where(f => f.NovelID == id && f.FollowerID == userId)
                .FirstOrDefaultAsync(cancellationToken);

            return await ToggleFollowAsync(follow, () => new Follow
            {
                FollowerID = userId,
                NovelID = id,
                UpdateTime = DateTime.Now,
                NovelMonthlyInvolver = false,
                ProfileMonthlyInvolver = false
            }, cancellationToken);
        }

        /// <summary>
        /// 已追蹤就取消，未追蹤就新增
        /// </summary>
        /// <param name="existingFollow">目前的追蹤紀錄；未追蹤時為 null</param>
        /// <param name="createFollow">建立新追蹤紀錄</param>
        /// <param name="cancellationToken">取消權杖</param>
        private async Task<IActionResult> ToggleFollowAsync(
            Follow existingFollow,
            Func<Follow> createFollow,
            CancellationToken cancellationToken)
        {
            if (existingFollow != null)
            {
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync(cancellationToken);
                return Ok();
            }

            _context.Follows.Add(createFollow());

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // 連點時另一個請求已經先建立追蹤，Unique 索引擋下重複的那筆，結果仍是「已追蹤」
            }

            return Ok();
        }
    }
}
