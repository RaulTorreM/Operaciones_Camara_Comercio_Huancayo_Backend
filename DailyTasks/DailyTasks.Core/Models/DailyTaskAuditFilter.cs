namespace DailyTasks.Core.Models;

/// <summary>Filtros de la auditoría global de cambios.</summary>
public sealed class DailyTaskAuditFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? UserId { get; set; }
    public int? TaskId { get; set; }
    public string? Action { get; set; }
    public string? Field { get; set; }
    public string? Query { get; set; }

    /// <summary>Solo cambios de título, vencimiento o responsable.</summary>
    public bool SensitiveOnly { get; set; }
}
