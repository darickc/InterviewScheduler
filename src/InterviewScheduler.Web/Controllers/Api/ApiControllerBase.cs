using InterviewScheduler.Core.Entities;
using InterviewScheduler.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewScheduler.Web.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = "Cookies")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IUserService UserService;

    private User? _cachedUser;

    protected ApiControllerBase(IUserService userService)
    {
        UserService = userService;
    }

    protected async Task<User?> GetCurrentUserAsync()
    {
        return _cachedUser ??= await UserService.GetCurrentUserAsync();
    }
}
