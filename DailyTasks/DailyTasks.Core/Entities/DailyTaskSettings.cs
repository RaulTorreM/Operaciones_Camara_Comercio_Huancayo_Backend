namespace DailyTasks.Core.Entities;

/// <summary>Configuración global del módulo de pendientes (fila única).</summary>
public class DailyTaskSettings
{
    public int Id { get; set; } = 1;

    /// <summary>Hora desde la que los usuarios regulares pueden crear pendientes (hora Perú).</summary>
    public TimeOnly CreationStartTime { get; set; } = new(8, 30);

    /// <summary>Hora hasta la que los usuarios regulares pueden crear pendientes (hora Perú).</summary>
    public TimeOnly CreationEndTime { get; set; } = new(19, 0);

    /// <summary>Permite crear pendientes también los sábados y domingos.</summary>
    public bool AllowWeekendCreation { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
