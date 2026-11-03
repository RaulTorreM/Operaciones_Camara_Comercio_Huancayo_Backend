namespace DailyTasks.Core.Enums;

/// <summary>Área/organización a la que pertenece un integrante.</summary>
public sealed record DailyTaskArea(string Code, string Name);

/// <summary>
/// Catálogo fijo de áreas del requerimiento. Se guarda el código en la base
/// y se expone el nombre legible en los DTO.
/// </summary>
public static class DailyTaskAreas
{
    public const string GestionAsociado = "GESTION_ASOCIADO";
    public const string Tesoreria = "TESORERIA";
    public const string ProtestosMoras = "PROTESTOS_MORAS";
    public const string Marketing = "MARKETING";
    public const string Secretaria = "SECRETARIA";
    public const string Logistica = "LOGISTICA";
    public const string CorteArbitraje = "CORTE_ARBITRAJE";
    public const string Contabilidad = "CONTABILIDAD";
    public const string Fidelizacion1 = "FIDELIZACION_1";
    public const string Fidelizacion2 = "FIDELIZACION_2";
    public const string GerenciaGeneral = "GERENCIA_GENERAL";

    public static IReadOnlyList<DailyTaskArea> All { get; } =
    [
        new(GestionAsociado, "GESTIÓN AL ASOCIADO"),
        new(Tesoreria, "TESORERÍA"),
        new(ProtestosMoras, "PROTESTOS Y MORAS"),
        new(Marketing, "MARKETING"),
        new(Secretaria, "SECRETARÍA"),
        new(Logistica, "LOGÍSTICA"),
        new(CorteArbitraje, "CORTE DE ARBITRAJE"),
        new(Contabilidad, "CONTABILIDAD"),
        new(Fidelizacion1, "FIDELIZACIÓN 1"),
        new(Fidelizacion2, "FIDELIZACIÓN 2"),
        new(GerenciaGeneral, "GERENCIA GENERAL")
    ];

    private static readonly Dictionary<string, string> Names =
        All.ToDictionary(a => a.Code, a => a.Name, StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Names.ContainsKey(code);

    public static string NameOf(string? code) =>
        code is not null && Names.TryGetValue(code, out var name) ? name : (code ?? "");
}
