using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [Route("api/v1/previews")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting("preview-auth")]
    public class PreviewsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Preview> _passwordHasher;

        public PreviewsApiController(
            ApplicationDbContext context,
            IPasswordHasher<Preview> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public class PreviewContentRequest
        {
            [Required(ErrorMessage = "請輸入閱讀密碼。")]
            public string Password { get; set; } = string.Empty;
        }

        public class PreviewRevokeRequest
        {
            [Required(ErrorMessage = "請輸入撤銷密碼。")]
            public string RevokePassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// 驗證密碼並取得小說試閱內容
        /// </summary>
        [HttpPost("{token}/content")]
        public async Task<IActionResult> GetContentAsync(string token, [FromBody] PreviewContentRequest request)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Token 不可為空。" });
            }

            var preview = await _context.Previews.FirstOrDefaultAsync(p => p.Token == token);
            if (preview == null)
            {
                return NotFound(new { message = "找不到此試閱或連結已失效。" });
            }

            if (preview.RevokedAt != null)
            {
                return StatusCode(StatusCodes.Status410Gone, new { message = "此試閱已被撤銷，無法閱讀。" });
            }

            if (preview.ExpiresAt <= DateTime.UtcNow)
            {
                return StatusCode(StatusCodes.Status410Gone, new { message = "此試閱已超過有效期限，無法閱讀。" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "請輸入閱讀密碼。" });
            }

            var verification = _passwordHasher.VerifyHashedPassword(preview, preview.PasswordHash, request.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "閱讀密碼錯誤，請重新輸入。" });
            }

            return Ok(new
            {
                title = preview.Title,
                contentHtml = preview.ContentHtml,
                createdAt = preview.CreatedAt,
                expiresAt = preview.ExpiresAt
            });
        }

        /// <summary>
        /// 驗證撤銷密碼並撤銷試閱
        /// </summary>
        [HttpPost("{token}/revoke")]
        public async Task<IActionResult> RevokeAsync(string token, [FromBody] PreviewRevokeRequest request)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Token 不可為空。" });
            }

            var preview = await _context.Previews.FirstOrDefaultAsync(p => p.Token == token);
            if (preview == null)
            {
                return NotFound(new { message = "找不到此試閱或連結已失效。" });
            }

            if (preview.RevokedAt != null)
            {
                return BadRequest(new { message = "此試閱先前已被撤銷。" });
            }

            if (preview.ExpiresAt <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "此試閱已過期，無需撤銷。" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.RevokePassword))
            {
                return BadRequest(new { message = "請輸入撤銷密碼。" });
            }

            var verification = _passwordHasher.VerifyHashedPassword(preview, preview.RevokePasswordHash, request.RevokePassword);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "撤銷密碼錯誤。" });
            }

            preview.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "此試閱已成功撤銷。" });
        }

        /// <summary>
        /// 查詢試閱基本狀態（不包含內容與機密資訊）
        /// </summary>
        [HttpGet("{token}/status")]
        public async Task<IActionResult> GetStatusAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Token 不可為空。" });
            }

            var preview = await _context.Previews.AsNoTracking().FirstOrDefaultAsync(p => p.Token == token);
            if (preview == null)
            {
                return NotFound(new { message = "找不到此試閱。" });
            }

            return Ok(new
            {
                isRevoked = preview.RevokedAt != null,
                isExpired = preview.ExpiresAt <= DateTime.UtcNow,
                expiresAt = preview.ExpiresAt,
                createdAt = preview.CreatedAt
            });
        }
    }
}
