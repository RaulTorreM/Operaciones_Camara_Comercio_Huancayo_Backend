using Inventario.Core.Entities;

namespace Inventario.Core.Interfaces;

public interface ISupplyRepository
{
    Task<List<Supply>> ListAsync();
    Task<Supply?> GetByIdAsync(int id);
    /// <summary>Bloquea el consumible hasta que termine la transacción actual.</summary>
    Task<Supply?> GetByIdForUpdateAsync(int id);
    Task<List<SupplyMovement>> GetMovementsAsync(int supplyId, int take);
    Task<int> CountLowAsync();
    Task AddAsync(Supply supply);
}
