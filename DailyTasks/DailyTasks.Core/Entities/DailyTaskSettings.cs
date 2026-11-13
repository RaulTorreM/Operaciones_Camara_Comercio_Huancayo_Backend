namespace DailyTasks.Core.Entities;

/// <summary>Configuración global del módulo de pendientes (fila única).</summary>
public class DailyTaskSettings
{
    public int Id { get; set; } = 1;

    /// <summary>Días hacia adelante en los que un usuario regular puede fijar el vencimiento.</summary>
    public int UserDueDateWindowDays { get; set; } = 7;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
