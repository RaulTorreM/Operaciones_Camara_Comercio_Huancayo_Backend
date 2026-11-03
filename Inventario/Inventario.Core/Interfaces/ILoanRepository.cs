using Inventario.Core.Entities;

namespace Inventario.Core.Interfaces;

public interface ILoanRepository
{
    Task<List<Loan>> ListAsync();
    Task<Loan?> GetByIdAsync(int id);
    /// <summary>Bloquea el préstamo hasta que termine la transacción actual.</summary>
    Task<Loan?> GetByIdForUpdateAsync(int id);
    Task<int> CountActiveAsync();
    Task<int> CountOverdueAsync(DateOnly today);
    Task AddAsync(Loan loan);
}
