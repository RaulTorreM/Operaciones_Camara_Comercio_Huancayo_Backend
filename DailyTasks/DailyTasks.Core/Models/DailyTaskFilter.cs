using DailyTasks.Core.Enums;

namespace DailyTasks.Core.Models;

/// <summary>Filtros compartidos por listado, tablero, calendario y reportes.</summary>
public sealed class DailyTaskFilter
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
    public string? Query { get; set; }
    public DateTime? UpdatedSince { get; set; }

    /// <summary>Fecha de hoy en Perú, usada para calcular vencidos.</summary>
    public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
}
