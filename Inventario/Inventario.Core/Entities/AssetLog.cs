namespace Inventario.Core.Entities;

public class AssetLog
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
}
