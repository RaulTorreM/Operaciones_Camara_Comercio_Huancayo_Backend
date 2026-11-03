using Inventario.Core.Entities;
using Inventario.Core.Interfaces;
using Inventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Repositories;

public class LoanRepository(InventarioDbContext db) : ILoanRepository
{
    public Task<List<Loan>> ListAsync() =>
        db.Loans.AsNoTracking().Include(l => l.Items).ThenInclude(i => i.Asset)
            .OrderByDescending(l => l.OutAt).Take(200).ToListAsync();

    public Task<Loan?> GetByIdAsync(int id) =>
        db.Loans.Include(l => l.Items).ThenInclude(i => i.Asset).FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Loan?> GetByIdForUpdateAsync(int id)
    {
        // Bloquea la fila del préstamo; las devoluciones del mismo préstamo se serializan.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM inventory.\"Loans\" WHERE \"Id\" = {id} FOR UPDATE");

        return await db.Loans.Include(l => l.Items).ThenInclude(i => i.Asset)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public Task<int> CountActiveAsync() => db.Loans.CountAsync(l => l.ReturnedAt == null);

    public Task<int> CountOverdueAsync(DateOnly today) =>
        db.Loans.CountAsync(l => l.ReturnedAt == null && l.DueDate < today);

    public async Task AddAsync(Loan loan) => await db.Loans.AddAsync(loan);
}
