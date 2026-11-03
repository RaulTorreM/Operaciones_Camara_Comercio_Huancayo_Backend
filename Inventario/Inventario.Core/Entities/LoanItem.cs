using Inventario.Core.Enums;

namespace Inventario.Core.Entities;

public class LoanItem
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public AssetCondition? ReturnCondition { get; set; }
}
