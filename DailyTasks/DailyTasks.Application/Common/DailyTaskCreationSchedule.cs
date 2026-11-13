namespace DailyTasks.Application.Common;

/// <summary>
/// Reglas del horario en que los usuarios regulares pueden crear pendientes.
/// Gerencia General no está sujeta a este horario.
/// </summary>
public static class DailyTaskCreationSchedule
{
    public static readonly TimeOnly DefaultStart = new(8, 30);
    public static readonly TimeOnly DefaultEnd = new(19, 0);

    public static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static bool IsWithin(
        DateOnly date,
        TimeOnly time,
        TimeOnly start,
        TimeOnly end,
        bool allowWeekend)
    {
        if (IsWeekend(date) && !allowWeekend) return false;
        return time >= start && time <= end;
    }
}
