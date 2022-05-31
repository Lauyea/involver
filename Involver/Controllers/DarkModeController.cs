using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    [ApiController]
    public class DarkModeController : ControllerBase
    {
        string _sessionKey = "_DarkMode";

        [HttpGet]
        public void Set()
        {
            string sessionValue = HttpContext.Session.GetString(_sessionKey);
            if (string.IsNullOrEmpty(sessionValue))
            {
                HttpContext.Session.SetString(_sessionKey, "On");
            }
            else if (sessionValue == "On")
            {
                HttpContext.Session.SetString(_sessionKey, "Off");
            }
            else
            {
                HttpContext.Session.SetString(_sessionKey, "On");
            }
        }
    }
}
