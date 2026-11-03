using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Interfaces;
using Inventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Repositories;

public class SupplyRepository(InventarioDbContext db) : ISupplyRepository
{
    public Task<List<Supply>> ListAsync() => db.Supplies.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

    public Task<Supply?> GetByIdAsync(int id) => db.Supplies.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Supply?> GetByIdForUpdateAsync(int id)
    {
        // Bloquea la fila del consumible; los movimientos simultáneos se serializan.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM inventory.\"Supplies\" WHERE \"Id\" = {id} FOR UPDATE");

        return await db.Supplies.FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task<List<SupplyMovement>> GetMovementsAsync(int supplyId, int take) =>
        db.SupplyMovements.AsNoTracking().Where(m => m.SupplyId == supplyId).OrderByDescending(m => m.At).Take(take).ToListAsync();

    public Task<int> CountLowAsync() =>
        db.Supplies.CountAsync(s =>
            (s.Mode == SupplyMode.Quantity && s.Stock <= s.MinStock) ||
            (s.Mode == SupplyMode.Visual && s.Level <= 1));

    public async Task AddAsync(Supply supply) => await db.Supplies.AddAsync(supply);
}
