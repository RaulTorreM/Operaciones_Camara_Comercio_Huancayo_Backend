using Inventario.Core.Enums;

namespace Inventario.Core.Entities;

/// <summary>Consumible del almacén. Por cantidad (hojas, lapiceros) o por nivel visual.</summary>
public class Supply
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "unid.";
    public SupplyMode Mode { get; set; } = SupplyMode.Quantity;
    public int Stock { get; set; }
    public int MinStock { get; set; }
    /// <summary>Solo modo visual: 0 agotado, 1 casi vacío, 2 mitad, 3 lleno.</summary>
    public int Level { get; set; } = 3;
    public List<SupplyMovement> Movements { get; set; } = new();

    public bool IsLow => Mode == SupplyMode.Quantity ? Stock <= MinStock : Level <= 1;
}
