namespace Inventario.Core.Models;

public record RoomStats(int Id, string Name, int Total, int Verified, int NotFound);
public record AssetCounts(int Total, int Verified, int NotFound, int Pending);
