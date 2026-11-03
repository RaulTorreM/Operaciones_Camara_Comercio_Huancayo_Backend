using Inventario.Core.Enums;

namespace Inventario.Core.Entities;

public class SupplyMovement
{
    public int Id { get; set; }
    public int SupplyId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public int ResultingStock { get; set; }
    public string? Destination { get; set; }
    public string User { get; set; } = "";
}
