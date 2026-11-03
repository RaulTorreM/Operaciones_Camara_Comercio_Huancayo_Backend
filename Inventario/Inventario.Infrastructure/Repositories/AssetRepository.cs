using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Interfaces;
using Inventario.Core.Models;
using Inventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Repositories;

public class AssetRepository(InventarioDbContext db) : IAssetRepository
{
    public async Task<List<Asset>> SearchAsync(string? q, int? roomId, VerificationStatus? status, AssetType? type, int take)
    {
        var query = db.Assets.AsNoTracking().Include(a => a.Room).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Name, pattern) || (a.Code != null && EF.Functions.ILike(a.Code, pattern)));
        }
        if (roomId.HasValue) query = roomId.Value == 0 ? query.Where(a => a.RoomId == null) : query.Where(a => a.RoomId == roomId.Value);
        if (status.HasValue) query = query.Where(a => a.Verification == status.Value);
        if (type.HasValue) query = query.Where(a => a.Type == type.Value);

        return await query.OrderBy(a => a.Name).Take(take).ToListAsync();
    }

    // Con seguimiento de cambios: los servicios modifican y luego guardan.
    public Task<Asset?> GetByIdAsync(int id) =>
        db.Assets.Include(a => a.Room).FirstOrDefaultAsync(a => a.Id == id);

    // Búsqueda sin distinguir mayúsculas ni espacios exteriores: "CCH-001" y " cch-001 " son la misma etiqueta.
    public Task<Asset?> GetByCodeAsync(string code)
    {
        var normalized = code.Trim().ToLower();
        return db.Assets.AsNoTracking().Include(a => a.Room)
            .FirstOrDefaultAsync(a => a.Code != null && a.Code.Trim().ToLower() == normalized);
    }

    public Task<List<Asset>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var list = ids.ToList();
        return db.Assets.Where(a => list.Contains(a.Id)).ToListAsync();
    }

    public Task<List<Asset>> GetByIdsForUpdateAsync(IEnumerable<int> ids)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return Task.FromResult(new List<Asset>());

        // Los ids ya son enteros, así que concatenarlos no permite inyección.
        // ORDER BY + FOR UPDATE bloquea todas las filas en el mismo orden y evita interbloqueos.
        // xmin es el token de concurrencia optimista y no lo incluye "SELECT *".
        var sql = $"SELECT a.*, a.xmin FROM inventory.\"Assets\" AS a " +
                  $"WHERE a.\"Id\" IN ({string.Join(",", list)}) " +
                  "ORDER BY a.\"Id\" FOR UPDATE";
        return db.Assets.FromSqlRaw(sql).ToListAsync();
    }

    public async Task<HashSet<string>> GetAllCodesAsync() =>
        (await db.Assets.Where(a => a.Code != null).Select(a => a.Code!).ToListAsync()).ToHashSet();

    public Task<List<AssetLog>> GetLogsAsync(int assetId, int take) =>
        db.AssetLogs.AsNoTracking().Where(l => l.AssetId == assetId).OrderByDescending(l => l.At).Take(take).ToListAsync();

    public async Task<AssetCounts> CountsAsync() => new(
        await db.Assets.CountAsync(),
        await db.Assets.CountAsync(a => a.Verification == VerificationStatus.Verified),
        await db.Assets.CountAsync(a => a.Verification == VerificationStatus.NotFound),
        await db.Assets.CountAsync(a => a.Verification == VerificationStatus.Pending));

    public async Task AddAsync(Asset asset) => await db.Assets.AddAsync(asset);

    public async Task AddRangeAsync(IEnumerable<Asset> assets) => await db.Assets.AddRangeAsync(assets);

    public void Remove(Asset asset) => db.Assets.Remove(asset);

    public Task<bool> HasLoanHistoryAsync(int assetId) =>
        db.LoanItems.AnyAsync(i => i.AssetId == assetId);
}
