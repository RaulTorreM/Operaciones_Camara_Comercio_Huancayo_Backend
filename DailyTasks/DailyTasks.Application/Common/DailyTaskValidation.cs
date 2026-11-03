using DailyTasks.Core.Exceptions;

namespace DailyTasks.Application.Common;

/// <summary>Validaciones simples compartidas por los servicios de DailyTasks.</summary>
public static class DailyTaskValidation
{
    public static void CheckEnum<TEnum>(TEnum? value, string message) where TEnum : struct, Enum
    {
        if (value.HasValue && !Enum.IsDefined(value.Value))
            throw new BusinessRuleException(message);
    }

    public static string Required(string? value, int max, string message)
    {
        var clean = value?.Trim();
        if (string.IsNullOrWhiteSpace(clean) || clean.Length > max)
            throw new BusinessRuleException(message);

        return clean;
    }

    public static string? Optional(string? value, int max, string message)
    {
        var clean = value?.Trim();
        if (string.IsNullOrEmpty(clean)) return null;
        if (clean.Length > max) throw new BusinessRuleException(message);

        return clean;
    }
}
