using DailyTasks.Application.DTOs.Auth;
using DailyTasks.Application.DTOs.Reports;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.DTOs.Users;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;

namespace DailyTasks.Application.Mappings;

public static class DailyTaskMappings
{
    public static DailyTaskUserDto ToDto(this DailyTaskUser user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Area,
        DailyTaskAreas.NameOf(user.Area),
        user.Role);

    public static DailyTaskDto ToDto(this DailyTask task)
    {
        var today = Common.DailyTaskTime.Today;
        var isOverdue = task.DeletedAt == null && task.Status != DailyTaskStatus.Done && task.DueDate < today;
        var daysOverdue = isOverdue ? today.DayNumber - task.DueDate.DayNumber : 0;

        return new DailyTaskDto(
            task.Id,
            $"DT-{task.Id}",
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.Area,
            DailyTaskAreas.NameOf(task.Area),
            task.AssignedToId,
            task.AssignedTo?.FullName ?? "",
            task.CreatedById,
            task.CreatedBy?.FullName ?? "",
            task.CreatedAt,
            task.CreatedOn,
            task.DueDate,
            task.UpdatedAt,
            task.CompletedAt,
            task.CompletedOn,
            isOverdue,
            daysOverdue,
            task.DeletedAt != null,
            task.DeletedAt,
            task.DeletedBy?.FullName);
    }

    public static DailyTaskCardDto ToCard(this DailyTask task)
    {
        var today = Common.DailyTaskTime.Today;
        var isOverdue = task.DeletedAt == null && task.Status != DailyTaskStatus.Done && task.DueDate < today;
        var daysOverdue = isOverdue ? today.DayNumber - task.DueDate.DayNumber : 0;

        return new DailyTaskCardDto(
            task.Id,
            $"DT-{task.Id}",
            task.Title,
            task.Status,
            task.Priority,
            task.Area,
            DailyTaskAreas.NameOf(task.Area),
            task.AssignedToId,
            task.AssignedTo?.FullName ?? "",
            task.DueDate,
            isOverdue,
            daysOverdue);
    }

    public static DailyTaskHistoryDto ToDto(this DailyTaskHistory history) => new(
        history.Id,
        history.ChangedAt,
        history.ChangedById,
        history.ChangedBy?.FullName ?? "",
        history.ChangeGroupId,
        history.Action,
        history.Field,
        history.OldValue,
        history.NewValue);

    public static DailyTaskAuditEntryDto ToAuditDto(this DailyTaskHistory history)
    {
        var area = history.Task?.Area ?? "";
        return new DailyTaskAuditEntryDto(
            history.Id,
            history.ChangedAt,
            history.ChangedById,
            history.ChangedBy?.FullName ?? "",
            history.ChangeGroupId,
            history.Action,
            history.Field,
            history.OldValue,
            history.NewValue,
            history.TaskId,
            $"DT-{history.TaskId}",
            history.Task?.Title ?? "Pendiente eliminado",
            area,
            DailyTaskAreas.NameOf(area),
            history.Task?.AssignedToId ?? 0,
            history.Task?.AssignedTo?.FullName ?? "");
    }

    public static Core.Models.DailyTaskFilter ToFilter(this DailyTaskQuery query) => new()
    {
        Status = query.Status,
        Priority = query.Priority,
        AssignedToId = query.AssignedToId,
        Area = string.IsNullOrWhiteSpace(query.Area) ? null : query.Area.Trim().ToUpperInvariant(),
        CreatedFrom = query.CreatedFrom,
        CreatedTo = query.CreatedTo,
        DueFrom = query.DueFrom,
        DueTo = query.DueTo,
        DueDate = query.DueDate,
        CompletedOn = query.CompletedOn,
        Overdue = query.Overdue,
        Query = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim(),
        UpdatedSince = query.UpdatedSince,
        Today = Common.DailyTaskTime.Today
    };
}
