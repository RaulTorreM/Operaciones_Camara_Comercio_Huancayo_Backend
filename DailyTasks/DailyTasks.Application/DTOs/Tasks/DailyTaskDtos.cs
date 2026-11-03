using DailyTasks.Core.Enums;

namespace DailyTasks.Application.DTOs.Tasks;

public sealed class DailyTaskQuery
{
    public DailyTaskStatus? Status { get; set; }
    public DailyTaskPriority? Priority { get; set; }
    public int? AssignedToId { get; set; }
    public string? Area { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public bool? Overdue { get; set; }
    public string? Q { get; set; }
    public DateTime? UpdatedSince { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public bool IncludeDeleted { get; set; }
}

public record CreateDailyTaskRequest(
    string? Title,
    string? Description,
    DailyTaskPriority? Priority,
    DateOnly? DueDate,
    int? AssignedToId);

public record UpdateDailyTaskRequest(
    string? Title,
    string? Description,
    DailyTaskPriority? Priority,
    DateOnly? DueDate);

public record ChangeDailyTaskStatusRequest(DailyTaskStatus? Status);

public record AssignDailyTaskRequest(int? AssignedToId);

public record DailyTaskDto(
    int Id,
    string Code,
    string Title,
    string? Description,
    DailyTaskStatus Status,
    DailyTaskPriority Priority,
    string Area,
    string AreaName,
    int AssignedToId,
    string AssignedToName,
    int CreatedById,
    string CreatedByName,
    DateTime CreatedAt,
    DateOnly CreatedOn,
    DateOnly DueDate,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    DateOnly? CompletedOn,
    bool IsOverdue,
    int DaysOverdue,
    bool IsDeleted,
    DateTime? DeletedAt,
    string? DeletedByName);

public record DailyTaskCardDto(
    int Id,
    string Code,
    string Title,
    DailyTaskStatus Status,
    DailyTaskPriority Priority,
    string Area,
    string AreaName,
    int AssignedToId,
    string AssignedToName,
    DateOnly DueDate,
    bool IsOverdue,
    int DaysOverdue);

public record DailyTaskHistoryDto(
    int Id,
    DateTime ChangedAt,
    int ChangedById,
    string ChangedByName,
    Guid ChangeGroupId,
    string Action,
    string? Field,
    string? OldValue,
    string? NewValue);

public record DailyTaskBoardDto(
    List<DailyTaskCardDto> Pending,
    List<DailyTaskCardDto> InProgress,
    List<DailyTaskCardDto> Done);

public record DailyTaskCalendarDayDto(DateOnly Date, List<DailyTaskCardDto> Tasks);

public record DailyTaskCalendarDto(
    DateOnly From,
    DateOnly To,
    List<DailyTaskCalendarDayDto> Days);

public record DailyTaskChangesDto(
    List<DailyTaskDto> Tasks,
    List<int> DeletedTaskIds);

public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
