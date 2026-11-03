using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskUnitOfWork(DailyTasksDbContext db) : IDailyTaskUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Ya hay una transacción activa.");

        _transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        catch
        {
            // Si la conexión ya se cayó no hay nada que revertir; se conserva la excepción original.
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
