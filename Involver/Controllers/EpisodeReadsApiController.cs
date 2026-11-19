using DataAccess.Data;

using Involver.Services.Recommendations;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers;

/// <summary>
/// 章節閱讀進度。章節頁看到結尾時呼叫，用來計算讀完率
/// </summary>
/// <param name="episodeReadService">章節閱讀紀錄</param>
/// <param name="userManager">帳號管理</param>
[Route("api/v1/episodes/{episodeId:int}/read")]
[ApiController]
[ValidateAntiForgeryToken]
public class EpisodeReadsApiController(
    IEpisodeReadService episodeReadService,
    UserManager<InvolverUser> userManager) : ControllerBase
{
    /// <summary>
    /// 更新閱讀進度
    /// </summary>
    public class ReadProgressRequest
    {
        /// <summary>是否讀到結尾；目前只接受 true</summary>
        public bool ReachedEnd { get; set; }
    }

    /// <summary>
    /// 更新目前會員對這個章節的閱讀進度
    /// </summary>
    /// <param name="episodeId">章節 ID</param>
    /// <param name="request">進度</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204；章節不存在或還沒開啟過時 404</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateReadAsync(int episodeId, [FromBody] ReadProgressRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!request.ReachedEnd)
        {
            return BadRequest();
        }

        var found = await episodeReadService.MarkReachedEndAsync(userId, episodeId, cancellationToken);
        return found ? NoContent() : NotFound();
    }
}
