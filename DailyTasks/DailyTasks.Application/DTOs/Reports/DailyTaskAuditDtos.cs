namespace DailyTasks.Application.DTOs.Reports;

public sealed class DailyTaskAuditQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? UserId { get; set; }
    public int? TaskId { get; set; }
    public string? Action { get; set; }
    public string? Field { get; set; }
    public string? Q { get; set; }

    /// <summary>Solo cambios de título, vencimiento o responsable.</summary>
    public bool SensitiveOnly { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public record DailyTaskAuditEntryDto(
    int Id,
    DateTime ChangedAt,
    int ChangedById,
    string ChangedByName,
    Guid ChangeGroupId,
    string Action,
    string? Field,
    string? OldValue,
    string? NewValue,
    int TaskId,
    string TaskCode,
    string TaskTitle,
    string Area,
    string AreaName,
    int AssignedToId,
    string AssignedToName);
