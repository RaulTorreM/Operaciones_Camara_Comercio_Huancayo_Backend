using Inventario.Core.Enums;

namespace Inventario.Application.DTOs.Supplies;

public record SupplyDto(int Id, string Name, string Unit, SupplyMode Mode, int Stock, int MinStock, int Level, bool IsLow);
public record SupplyRequest(string? Name, string? Unit, SupplyMode? Mode, int? Stock, int? MinStock, int? Level);
public record MovementRequest(MovementType? Type, int Quantity, string? Destination);
public record LevelRequest(int Level);
public record SupplyMovementDto(DateTime At, MovementType Type, int Quantity, int ResultingStock, string? Destination, string User);
