namespace DailyTasks.Core.Entities;

/// <summary>Configuración global del módulo de pendientes (fila única).</summary>
public class DailyTaskSettings
{
    public int Id { get; set; } = 1;

    /// <summary>
    /// Cantidad de días de la ventana de creación configurada por Gerencia.
    /// Se conserva el nombre de columna anterior para no romper la base existente.
    /// </summary>
    public int UserDueDateWindowDays { get; set; } = 7;

    /// <summary>Último día en que los usuarios pueden crear pendientes (inclusive).</summary>
    public DateOnly? CreationWindowEndsOn { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
