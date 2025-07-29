using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewScheduler.Web.Controllers;

[Route("[controller]/[action]")]
public class AuthenticationController : Controller
{
    [HttpGet("/signin-google")]
    public IActionResult SignIn()
    {
        var properties = new AuthenticationProperties 
        { 
            RedirectUri = "/"
        };
        return Challenge(properties, "Google");
    }

    [HttpGet("/signout-google")]
    public new async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

}