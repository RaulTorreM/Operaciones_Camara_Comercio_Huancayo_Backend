namespace DailyTasks.Core.Entities;

/// <summary>Configuración global del módulo de pendientes (fila única).</summary>
public class DailyTaskSettings
{
    public int Id { get; set; } = 1;

    /// <summary>
    /// Máximo de pendientes activos que puede tener un usuario regular.
    /// Se conserva el nombre de columna anterior para no romper la base existente.
    /// </summary>
    public int UserDueDateWindowDays { get; set; } = 7;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
