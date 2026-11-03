using DailyTasks.Core.Enums;

namespace DailyTasks.Core.Entities;

/// <summary>Pendiente diario del módulo DailyTasks.</summary>
public class DailyTask
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DailyTaskStatus Status { get; set; } = DailyTaskStatus.Pending;
    public DailyTaskPriority Priority { get; set; } = DailyTaskPriority.Medium;

    /// <summary>Área del responsable al momento de crear o reasignar.</summary>
    public string Area { get; set; } = "";

    public int CreatedById { get; set; }
    public DailyTaskUser? CreatedBy { get; set; }

    public int AssignedToId { get; set; }
    public DailyTaskUser? AssignedTo { get; set; }

    /// <summary>Fecha y hora exacta del registro (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de creación en Perú, para filtros y reportes.</summary>
    public DateOnly CreatedOn { get; set; }

    /// <summary>Fecha de vencimiento en Perú.</summary>
    public DateOnly DueDate { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }
    public DailyTaskUser? DeletedBy { get; set; }

    public List<DailyTaskHistory> History { get; set; } = new();
}
