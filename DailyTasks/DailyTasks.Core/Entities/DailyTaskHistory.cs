namespace DailyTasks.Core.Entities;

/// <summary>Registro append-only de cambios de un pendiente.</summary>
public class DailyTaskHistory
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    public DailyTask? Task { get; set; }

    public int ChangedById { get; set; }
    public DailyTaskUser? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Agrupa los cambios realizados en una misma edición.</summary>
    public Guid ChangeGroupId { get; set; } = Guid.NewGuid();

    /// <summary>CREATED, UPDATED, STATUS_CHANGED, ASSIGNED, DELETED, RESTORED.</summary>
    public string Action { get; set; } = "";

    /// <summary>Title, Description, Priority, DueDate, Status o AssignedTo.</summary>
    public string? Field { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
