namespace Inventario.Application.Common;

public static class PeruTime
{
    /// <summary>Fecha de hoy en Perú (UTC-5), para comparar fechas de devolución.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
