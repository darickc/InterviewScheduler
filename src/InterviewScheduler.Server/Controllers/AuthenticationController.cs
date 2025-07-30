using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewScheduler.Server.Controllers
{
    [Route("Account")]
    public class AuthenticationController : Controller
    {
        [HttpGet("Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = Url.Action("LoginCallback", new { returnUrl })
            }, "Google");
        }

        [HttpGet("LoginCallback")]
        public IActionResult LoginCallback(string? returnUrl = null)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpGet("Logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return LocalRedirect("/");
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}