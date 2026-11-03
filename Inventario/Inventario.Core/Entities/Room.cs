namespace Inventario.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Asset> Assets { get; set; } = new();
}
