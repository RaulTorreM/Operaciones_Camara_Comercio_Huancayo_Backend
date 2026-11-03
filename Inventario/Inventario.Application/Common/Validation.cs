using Inventario.Core.Exceptions;

namespace Inventario.Application.Common;

/// <summary>Validaciones simples compartidas por los servicios del inventario.</summary>
public static class Validation
{
    /// <summary>Indica si el valor es nulo o un valor real del enum.</summary>
    public static bool IsValidEnum<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        !value.HasValue || Enum.IsDefined(value.Value);

    /// <summary>Rechaza valores de enum que no existen (por ejemplo, 99 enviado por un cliente).</summary>
    public static void CheckEnum<TEnum>(TEnum? value, string message) where TEnum : struct, Enum
    {
        if (!IsValidEnum(value))
            throw new BusinessRuleException(message);
    }

    /// <summary>Rechaza textos que superan el largo permitido por la base de datos.</summary>
    public static void MaxLength(string? value, int max, string message)
    {
        if (value is not null && value.Length > max)
            throw new BusinessRuleException(message);
    }
}
