using Inventario.Core.Entities;
using Inventario.Core.Models;

namespace Inventario.Core.Interfaces;

public interface IRoomRepository
{
    Task<List<RoomStats>> ListWithStatsAsync();
    Task<List<Room>> GetAllAsync();
    Task<Room?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name);
    Task AddAsync(Room room);
    Task<int> ResetVerificationAsync(int roomId);
}
