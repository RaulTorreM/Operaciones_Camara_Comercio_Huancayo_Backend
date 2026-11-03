using DailyTasks.Application.DTOs.Auth;
using DailyTasks.Application.DTOs.Reports;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.DTOs.Users;

namespace DailyTasks.Application.Interfaces;

public interface IDailyTaskAuthService
{
    Task<DailyTaskLoginResponse> LoginAsync(DailyTaskLoginRequest request);
    Task<DailyTaskUserDto> GetMeAsync();
    Task ChangePasswordAsync(ChangePasswordRequest request);
}

public interface IDailyTaskService
{
    Task<PagedResult<DailyTaskDto>> ListAsync(DailyTaskQuery query);
    Task<DailyTaskDto> GetAsync(int id);
    Task<DailyTaskDto> CreateAsync(CreateDailyTaskRequest request);
    Task<DailyTaskDto> UpdateAsync(int id, UpdateDailyTaskRequest request);
    Task<DailyTaskDto> ChangeStatusAsync(int id, ChangeDailyTaskStatusRequest request);
    Task<DailyTaskDto> AssignAsync(int id, AssignDailyTaskRequest request);
    Task DeleteAsync(int id);
    Task<DailyTaskDto> RestoreAsync(int id);
    Task<List<DailyTaskHistoryDto>> HistoryAsync(int id);
    Task<DailyTaskBoardDto> BoardAsync(DailyTaskQuery query);
    Task<DailyTaskCalendarDto> CalendarAsync(DateOnly? from, DateOnly? to, DailyTaskQuery query);
    Task<DailyTaskChangesDto> ChangesAsync(DateTime? sinceUtc);
    Task<List<DailyTaskUserDto>> ListUsersAsync(string? area);
    Task<List<DailyTaskAreaDto>> ListAreasAsync();
}

public interface IDailyTaskReportService
{
    Task<DailyTaskSummaryDto> GetSummaryAsync();
    Task<DailyTaskReportDto> GetReportAsync(DailyTaskReportQuery query);
    Task<DailyTaskReportDto> GetMyReportAsync(DailyTaskReportQuery query);
    Task<PagedResult<DailyTaskAuditEntryDto>> GetAuditAsync(DailyTaskAuditQuery query);
}
