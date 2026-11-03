using Inventario.Application.Common;
using Inventario.Application.DTOs.Loans;
using Inventario.Application.Interfaces;
using Inventario.Application.Mappings;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class LoanService(ILoanRepository loans, IAssetRepository assets, IUnitOfWork uow, ICurrentUser user) : ILoanService
{
    private const int MaxBorrowerLength = 200;

    public async Task<List<LoanDto>> ListAsync() =>
        (await loans.ListAsync()).Select(l => l.ToDto()).ToList();

    public async Task<int> CreateAsync(CreateLoanRequest r)
    {
        var borrower = PeruTime.Clean(r.Borrower) ?? throw new BusinessRuleException("Indica a quién se presta.");
        Validation.MaxLength(borrower, MaxBorrowerLength, "El nombre del prestatario no debe superar 200 caracteres.");

        var ids = r.AssetIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0) throw new BusinessRuleException("Elige al menos un bien.");
        if (r.DueDate < PeruTime.Today) throw new BusinessRuleException("La fecha de devolución no puede ser anterior a hoy.");

        // Se bloquean las filas de los bienes hasta confirmar el préstamo. Así dos usuarios
        // que prestan el mismo bien a la vez no pueden crear dos préstamos activos.
        await uow.BeginTransactionAsync();
        try
        {
            var found = await assets.GetByIdsForUpdateAsync(ids);
            if (found.Count != ids.Count) throw new BusinessRuleException("Alguno de los bienes ya no existe.");

            var busy = found.Where(a => a.OnLoan || a.Condition == AssetCondition.Decommissioned)
                .Select(a => a.Name).ToList();
            if (busy.Count > 0) throw new ConflictException("No disponibles: " + string.Join(", ", busy));

            var lots = found.Where(a => a.Type != AssetType.Individual).Select(a => a.Name).ToList();
            if (lots.Count > 0)
                throw new BusinessRuleException("Solo se pueden prestar bienes individuales: " + string.Join(", ", lots));

            var loan = new Loan
            {
                Borrower = borrower,
                DueDate = r.DueDate,
                Notes = PeruTime.Clean(r.Notes),
                User = user.Name
            };
            foreach (var asset in found)
            {
                asset.OnLoan = true;
                loan.Items.Add(new LoanItem { Asset = asset });
                asset.Logs.Add(new AssetLog
                {
                    At = DateTime.UtcNow, User = user.Name, Action = "Préstamo",
                    Detail = $"Prestado a {borrower} hasta {r.DueDate:dd/MM/yyyy}"
                });
            }

            await loans.AddAsync(loan);
            await uow.SaveChangesAsync();
            await uow.CommitAsync();
            return loan.Id;
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public async Task<ReturnLoanResultDto> ReturnAsync(int id, ReturnLoanRequest r)
    {
        var requested = r.Items;
        if (requested is null || requested.Count == 0)
            throw new BusinessRuleException("Marca al menos un bien para devolver.");

        var conditions = new Dictionary<int, AssetCondition>();
        foreach (var item in requested)
        {
            Validation.CheckEnum<AssetCondition>(item.Condition, "Estado del bien no válido.");
            if (!conditions.TryAdd(item.AssetId, item.Condition))
                throw new BusinessRuleException("Hay un bien repetido en la devolución.");
        }

        // Bloquea el préstamo: dos devoluciones simultáneas del mismo préstamo se serializan
        // y la segunda ve el estado actualizado (incluido si ya quedó completo).
        await uow.BeginTransactionAsync();
        try
        {
            var loan = await loans.GetByIdForUpdateAsync(id)
                ?? throw new NotFoundException("No se encontró el préstamo.");
            if (loan.ReturnedAt != null) throw new BusinessRuleException("Este préstamo ya fue devuelto.");

            var active = new Dictionary<int, LoanItem>();
            foreach (var item in loan.Items.Where(i => i.ReturnedAt == null))
            {
                if (!active.TryAdd(item.AssetId, item))
                    throw new ConflictException("Hay un bien duplicado en este préstamo. Revisa el historial.");
            }

            foreach (var assetId in conditions.Keys)
            {
                if (active.ContainsKey(assetId)) continue;

                var belongs = loan.Items.Any(i => i.AssetId == assetId);
                throw new BusinessRuleException(belongs
                    ? "Uno de los bienes seleccionados ya fue devuelto."
                    : "Uno de los bienes no pertenece a este préstamo.");
            }

            var now = DateTime.UtcNow;
            foreach (var item in active.Values)
            {
                if (!conditions.TryGetValue(item.AssetId, out var condition)) continue;

                var asset = item.Asset
                    ?? throw new BusinessRuleException("No se encontró uno de los bienes del préstamo.");
                item.ReturnedAt = now;
                item.ReturnCondition = condition;
                asset.OnLoan = false;
                asset.Condition = condition;
                asset.UpdatedAt = now;
                asset.Logs.Add(new AssetLog
                {
                    At = now, User = user.Name, Action = "Devolución",
                    Detail = $"Devuelto por {loan.Borrower} · estado: {condition}"
                });
            }

            if (loan.Items.All(i => i.ReturnedAt != null)) loan.ReturnedAt = now;

            await uow.SaveChangesAsync();
            await uow.CommitAsync();
            return new ReturnLoanResultDto(loan.ReturnedAt != null);
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }
}
