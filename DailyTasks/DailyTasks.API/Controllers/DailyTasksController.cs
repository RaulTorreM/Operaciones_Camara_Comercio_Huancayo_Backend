using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.Interfaces;
using DailyTasks.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyTasks.API.Controllers;

[ApiController]
[Route("api/dailytasks/tasks")]
[Authorize(Policy = DailyTasksAuth.UserPolicy)]
public class DailyTasksController(IDailyTaskService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DailyTaskDto>> Create([FromBody] CreateDailyTaskRequest request) =>
        Ok(await service.CreateAsync(request));

    [HttpGet]
    public async Task<ActionResult<PagedResult<DailyTaskDto>>> List([FromQuery] DailyTaskQuery query) =>
        Ok(await service.ListAsync(query));

    [HttpGet("board")]
    public async Task<ActionResult<DailyTaskBoardDto>> Board([FromQuery] DailyTaskQuery query) =>
        Ok(await service.BoardAsync(query));

    [HttpGet("calendar")]
    public async Task<ActionResult<DailyTaskCalendarDto>> Calendar(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] DailyTaskQuery query) =>
        Ok(await service.CalendarAsync(from, to, query));

    [HttpGet("changes")]
    public async Task<ActionResult<DailyTaskChangesDto>> Changes([FromQuery] DateTime? since) =>
        Ok(await service.ChangesAsync(since));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DailyTaskDto>> Get(int id) =>
        Ok(await service.GetAsync(id));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DailyTaskDto>> Update(int id, [FromBody] UpdateDailyTaskRequest request) =>
        Ok(await service.UpdateAsync(id, request));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<DailyTaskDto>> ChangeStatus(int id, [FromBody] ChangeDailyTaskStatusRequest request) =>
        Ok(await service.ChangeStatusAsync(id, request));

    [HttpPatch("{id:int}/assignee")]
    [Authorize(Policy = DailyTasksAuth.ManagerPolicy)]
    public async Task<ActionResult<DailyTaskDto>> Assign(int id, [FromBody] AssignDailyTaskRequest request) =>
        Ok(await service.AssignAsync(id, request));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = DailyTasksAuth.ManagerPolicy)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = DailyTasksAuth.ManagerPolicy)]
    public async Task<ActionResult<DailyTaskDto>> Restore(int id) =>
        Ok(await service.RestoreAsync(id));

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<DailyTaskHistoryDto>>> History(int id) =>
        Ok(await service.HistoryAsync(id));
}
