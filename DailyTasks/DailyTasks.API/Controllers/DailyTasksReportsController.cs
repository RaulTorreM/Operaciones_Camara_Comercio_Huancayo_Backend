using DailyTasks.Application.DTOs.Reports;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.Interfaces;
using DailyTasks.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyTasks.API.Controllers;

[ApiController]
[Route("api/dailytasks")]
[Authorize(Policy = DailyTasksAuth.UserPolicy)]
public class DailyTasksReportsController(IDailyTaskReportService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DailyTaskSummaryDto>> Summary() =>
        Ok(await service.GetSummaryAsync());

    [HttpGet("reports")]
    [Authorize(Policy = DailyTasksAuth.ManagerPolicy)]
    public async Task<ActionResult<DailyTaskReportDto>> Report([FromQuery] DailyTaskReportQuery query) =>
        Ok(await service.GetReportAsync(query));

    [HttpGet("reports/me")]
    public async Task<ActionResult<DailyTaskReportDto>> MyReport([FromQuery] DailyTaskReportQuery query) =>
        Ok(await service.GetMyReportAsync(query));

    [HttpGet("audit")]
    [Authorize(Policy = DailyTasksAuth.ManagerPolicy)]
    public async Task<ActionResult<PagedResult<DailyTaskAuditEntryDto>>> Audit([FromQuery] DailyTaskAuditQuery query) =>
        Ok(await service.GetAuditAsync(query));
}
