namespace DailyTasks.Application.Common;

/// <summary>Fechas y horas del módulo. Se guarda UTC y se opera con fecha Perú (UTC-5).</summary>
public static class DailyTaskTime
{
    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

    public static DateOnly ToPeruDate(DateTime utc) => DateOnly.FromDateTime(utc.AddHours(-5));
}
