using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Models;

namespace Inventario.Core.Interfaces;

public interface IAssetRepository
{
    Task<List<Asset>> SearchAsync(string? q, int? roomId, VerificationStatus? status, AssetType? type, int take);
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset?> GetByCodeAsync(string code);
    Task<List<Asset>> GetByIdsAsync(IEnumerable<int> ids);
    /// <summary>Bloquea las filas de los bienes indicados hasta que termine la transacción actual.</summary>
    Task<List<Asset>> GetByIdsForUpdateAsync(IEnumerable<int> ids);
    Task<HashSet<string>> GetAllCodesAsync();
    Task<List<AssetLog>> GetLogsAsync(int assetId, int take);
    Task<AssetCounts> CountsAsync();
    Task AddAsync(Asset asset);
    Task AddRangeAsync(IEnumerable<Asset> assets);
    void Remove(Asset asset);
    Task<bool> HasLoanHistoryAsync(int assetId);
}
