namespace DailyTasks.Application.Common;

/// <summary>Fechas y horas del módulo. Se guarda UTC y se opera con fecha Perú (UTC-5).</summary>
public static class DailyTaskTime
{
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Fecha y hora actual de Perú (UTC-5, sin horario de verano).</summary>
    public static DateTime NowPeru => DateTime.UtcNow.AddHours(-5);

    public static DateOnly Today => DateOnly.FromDateTime(NowPeru);

    public static DateOnly ToPeruDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(-5));
}
