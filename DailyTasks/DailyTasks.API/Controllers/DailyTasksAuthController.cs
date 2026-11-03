using DailyTasks.Application.DTOs.Auth;
using DailyTasks.Application.DTOs.Users;
using DailyTasks.Application.Interfaces;
using DailyTasks.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DailyTasks.API.Controllers;

[ApiController]
[Route("api/dailytasks/auth")]
public class DailyTasksAuthController(IDailyTaskAuthService service) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<DailyTaskLoginResponse>> Login([FromBody] DailyTaskLoginRequest request) =>
        Ok(await service.LoginAsync(request));

    [HttpGet("me")]
    [Authorize(Policy = DailyTasksAuth.UserPolicy)]
    public async Task<ActionResult<DailyTaskUserDto>> Me() =>
        Ok(await service.GetMeAsync());

    [HttpPost("change-password")]
    [Authorize(Policy = DailyTasksAuth.UserPolicy)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await service.ChangePasswordAsync(request);
        return NoContent();
    }
}
