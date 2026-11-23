using DataAccess.Data;
using DataAccess.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers;

/// <summary>
/// 追蹤／取消追蹤作者與小說。PUT 與 DELETE 都可以重複呼叫，連點也不會出錯。
/// </summary>
[Route("api/v1/follows")]
[ApiController]
[AutoValidateAntiforgeryToken]
public class FollowsApiController(ApplicationDbContext context, UserManager<InvolverUser> userManager) : ControllerBase
{
    /// <summary>
    /// 追蹤作者
    /// </summary>
    /// <param name="id">作者 ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>追蹤後的狀態；找不到作者時 404</returns>
    [HttpPut("authors/{id}")]
    public async Task<IActionResult> FollowAuthorAsync(string id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!await context.Profiles.AnyAsync(p => p.ProfileID == id, cancellationToken))
        {
            return NotFound();
        }

        var followed = await context.Follows
            .AnyAsync(f => f.ProfileID == id && f.FollowerID == userId, cancellationToken);

        return await AddFollowAsync(followed, new Follow
        {
            FollowerID = userId,
            ProfileID = id,
            UpdateTime = DateTime.Now,
            NovelMonthlyInvolver = false,
            ProfileMonthlyInvolver = false
        }, cancellationToken);
    }

    /// <summary>
    /// 取消追蹤作者
    /// </summary>
    /// <param name="id">作者 ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>取消後的狀態</returns>
    [HttpDelete("authors/{id}")]
    public async Task<IActionResult> UnfollowAuthorAsync(string id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var follow = await context.Follows
            .Where(f => f.ProfileID == id && f.FollowerID == userId)
            .FirstOrDefaultAsync(cancellationToken);

        return await RemoveFollowAsync(follow, cancellationToken);
    }

    /// <summary>
    /// 追蹤小說
    /// </summary>
    /// <param name="id">NovelID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>追蹤後的狀態；找不到小說時 404</returns>
    [HttpPut("novels/{id:int}")]
    public async Task<IActionResult> FollowNovelAsync(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!await context.Novels.AnyAsync(n => n.NovelID == id, cancellationToken))
        {
            return NotFound();
        }

        var followed = await context.Follows
            .AnyAsync(f => f.NovelID == id && f.FollowerID == userId, cancellationToken);

        return await AddFollowAsync(followed, new Follow
        {
            FollowerID = userId,
            NovelID = id,
            UpdateTime = DateTime.Now,
            NovelMonthlyInvolver = false,
            ProfileMonthlyInvolver = false
        }, cancellationToken);
    }

    /// <summary>
    /// 取消追蹤小說
    /// </summary>
    /// <param name="id">NovelID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>取消後的狀態</returns>
    [HttpDelete("novels/{id:int}")]
    public async Task<IActionResult> UnfollowNovelAsync(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var follow = await context.Follows
            .Where(f => f.NovelID == id && f.FollowerID == userId)
            .FirstOrDefaultAsync(cancellationToken);

        return await RemoveFollowAsync(follow, cancellationToken);
    }

    /// <summary>
    /// 尚未追蹤時新增追蹤紀錄
    /// </summary>
    /// <param name="alreadyFollowed">是否已經追蹤</param>
    /// <param name="follow">要新增的追蹤紀錄</param>
    /// <param name="cancellationToken">取消權杖</param>
    private async Task<IActionResult> AddFollowAsync(bool alreadyFollowed, Follow follow, CancellationToken cancellationToken)
    {
        if (!alreadyFollowed)
        {
            context.Follows.Add(follow);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // 連點時另一個請求已經先建立追蹤，Unique 索引擋下重複的那筆，結果仍是「已追蹤」
            }
        }

        return Ok(new { followed = true });
    }

    /// <summary>
    /// 有追蹤紀錄時刪除
    /// </summary>
    /// <param name="follow">目前的追蹤紀錄；未追蹤時為 null</param>
    /// <param name="cancellationToken">取消權杖</param>
    private async Task<IActionResult> RemoveFollowAsync(Follow follow, CancellationToken cancellationToken)
    {
        if (follow != null)
        {
            context.Follows.Remove(follow);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // 連點時另一個請求已經先刪除，結果仍是「未追蹤」
            }
        }

        return Ok(new { followed = false });
    }
}
