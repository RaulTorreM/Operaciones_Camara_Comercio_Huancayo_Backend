using Inventario.Core.Enums;

namespace Inventario.Application.DTOs.Assets;

public record AssetListItemDto(
    int Id, string? Code, string Name, string? Category, AssetType Type, int Quantity,
    int? RoomId, string? Room, AssetCondition Condition, VerificationStatus Verification,
    bool OnLoan, bool HasPhoto);

public record AssetLogDto(DateTime At, string User, string Action, string? Detail);

public record AssetDetailDto(
    int Id, string? Code, string Name, string? Category, AssetType Type, int Quantity,
    int? RoomId, string? Room, AssetCondition Condition, VerificationStatus Verification,
    bool OnLoan, string? Notes, DateTime? LastVerifiedAt, string? PhotoUrl, List<AssetLogDto> History);

public record AssetCodeLookupDto(
    int Id, string Name, AssetType Type, int? RoomId, string? Room, VerificationStatus Verification);

/// <summary>
/// Datos mínimos que se muestran en la consulta pública por etiqueta (sin iniciar sesión).
/// No incluye notas, historial, fotos ni datos del personal.
/// </summary>
public record PublicAssetDto(
    string? Code, string Name, string? Category, AssetType Type, int Quantity,
    string? Room, AssetCondition Condition, VerificationStatus Verification, bool OnLoan);

/// <summary>Cuerpo para crear o editar un bien. En la importación se usa RoomName en vez de RoomId.</summary>
public record AssetRequest(
    string? Code, string? Name, string? Category, AssetType? Type, int? Quantity, int? RoomId,
    string? RoomName, AssetCondition? Condition, VerificationStatus? Verification,
    string? PhotoDataUrl, string? Notes);

public record VerifyRequest(VerificationStatus? Result, int? RoomId, int? Quantity);
public record ImportResultDto(int Created, int Skipped);
public record CreatedDto(int Id);
