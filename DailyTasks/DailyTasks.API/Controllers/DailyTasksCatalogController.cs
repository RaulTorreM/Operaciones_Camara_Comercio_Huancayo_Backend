using DailyTasks.Application.DTOs.Users;
using DailyTasks.Application.Interfaces;
using DailyTasks.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyTasks.API.Controllers;

[ApiController]
[Route("api/dailytasks")]
[Authorize(Policy = DailyTasksAuth.UserPolicy)]
public class DailyTasksCatalogController(IDailyTaskService service) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<DailyTaskUserDto>>> Users([FromQuery] string? area) =>
        Ok(await service.ListUsersAsync(area));

    [HttpGet("areas")]
    public async Task<ActionResult<List<DailyTaskAreaDto>>> Areas() =>
        Ok(await service.ListAreasAsync());
}
