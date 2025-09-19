using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    [ApiController]
    public class DarkModeController : ControllerBase
    {
        readonly string _sessionKey = "_DarkMode";

        /// <summary>
        /// Server-side Session
        /// </summary>
        //[HttpGet]
        //public void Set()
        //{
        //    string sessionValue = HttpContext.Session.GetString(_sessionKey);
        //    if (string.IsNullOrEmpty(sessionValue))
        //    {
        //        HttpContext.Session.SetString(_sessionKey, "On");
        //    }
        //    else if (sessionValue == "On")
        //    {
        //        HttpContext.Session.SetString(_sessionKey, "Off");
        //    }
        //    else
        //    {
        //        HttpContext.Session.SetString(_sessionKey, "On");
        //    }
        //}

        /// <summary>
        /// Client-side Cookie
        /// </summary>
        [HttpGet]
        public void Set()
        {
            string cookieValue = Request.Cookies[_sessionKey];
            string newValue = (cookieValue == "On") ? "Off" : "On";
            Response.Cookies.Append(_sessionKey, newValue, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1), // 可依需求調整
                HttpOnly = false, // 若要讓 JS 也能存取，設為 false
                Secure = true,    // 若只允許 HTTPS，設為 true
                SameSite = SameSiteMode.Lax
            });
        }
    }
}