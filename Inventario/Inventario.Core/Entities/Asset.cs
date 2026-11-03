using Inventario.Core.Enums;

namespace Inventario.Core.Entities;

/// <summary>Bien del inventario. Individual (con etiqueta) o por lote (cantidad por sala).</summary>
public class Asset
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public AssetType Type { get; set; } = AssetType.Individual;
    public int Quantity { get; set; } = 1;
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public VerificationStatus Verification { get; set; } = VerificationStatus.Pending;
    public bool OnLoan { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<AssetLog> Logs { get; set; } = new();
}
