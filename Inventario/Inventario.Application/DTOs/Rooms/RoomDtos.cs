namespace Inventario.Application.DTOs.Rooms;

public record RoomDto(int Id, string Name, int Total, int Verified, int NotFound);
public record RoomRequest(string? Name);
public record ResetResultDto(int Reset);
