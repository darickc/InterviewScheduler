using InterviewScheduler.Core.Interfaces;
using InterviewScheduler.Web.Mappings;
using Microsoft.AspNetCore.Mvc;

namespace InterviewScheduler.Web.Controllers.Api;

[Route("api/user")]
public class UserController : ApiControllerBase
{
    public UserController(IUserService userService) : base(userService) { }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();
        return Ok(user.ToDto());
    }
}
