using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Interfaces;
using Inventario.Core.Models;
using Inventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Repositories;

public class RoomRepository(InventarioDbContext db) : IRoomRepository
{
    public Task<List<RoomStats>> ListWithStatsAsync() =>
        db.Rooms.AsNoTracking().OrderBy(r => r.Name).Select(r => new RoomStats(
            r.Id, r.Name,
            r.Assets.Count(),
            r.Assets.Count(a => a.Verification == VerificationStatus.Verified),
            r.Assets.Count(a => a.Verification == VerificationStatus.NotFound))).ToListAsync();

    public Task<List<Room>> GetAllAsync() => db.Rooms.ToListAsync();

    public Task<Room?> GetByIdAsync(int id) => db.Rooms.FirstOrDefaultAsync(r => r.Id == id);

    public Task<bool> NameExistsAsync(string name)
    {
        var normalized = name.Trim().ToLower();
        return db.Rooms.AnyAsync(r => r.Name.Trim().ToLower() == normalized);
    }

    public async Task AddAsync(Room room) => await db.Rooms.AddAsync(room);

    public Task<int> ResetVerificationAsync(int roomId)
    {
        var now = DateTime.UtcNow;
        return db.Assets.Where(a => a.RoomId == roomId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Verification, VerificationStatus.Pending)
                .SetProperty(a => a.UpdatedAt, now));
    }
}
