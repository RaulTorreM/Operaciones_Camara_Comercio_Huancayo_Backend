using Inventario.Application.Common;
using Inventario.Application.DTOs.Supplies;
using Inventario.Application.Interfaces;
using Inventario.Application.Mappings;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Inventario.Core.Exceptions;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class SupplyService(ISupplyRepository supplies, IUnitOfWork uow, ICurrentUser user) : ISupplyService
{
    private const int MaxNameLength = 200;

    public async Task<List<SupplyDto>> ListAsync() =>
        (await supplies.ListAsync()).Select(s => s.ToDto()).ToList();

    public async Task<SupplyDto> CreateAsync(SupplyRequest r)
    {
        Validation.CheckEnum(r.Mode, "Modo de consumible no válido.");

        var name = PeruTime.Clean(r.Name) ?? throw new BusinessRuleException("El nombre es obligatorio.");
        Validation.MaxLength(name, MaxNameLength, "El nombre no debe superar 200 caracteres.");

        var visual = (r.Mode ?? SupplyMode.Quantity) == SupplyMode.Visual;

        if (!visual && r.Stock is < 0)
            throw new BusinessRuleException("El stock no puede ser negativo.");
        if (!visual && r.MinStock is < 0)
            throw new BusinessRuleException("El stock mínimo no puede ser negativo.");
        if (r.Level is < 0 or > 3)
            throw new BusinessRuleException("El nivel debe estar entre 0 y 3.");

        var supply = new Supply
        {
            Name = name,
            Unit = PeruTime.Clean(r.Unit) ?? (visual ? "" : "unid."),
            Mode = visual ? SupplyMode.Visual : SupplyMode.Quantity,
            Stock = visual ? 0 : r.Stock ?? 0,
            MinStock = visual ? 0 : r.MinStock ?? 0,
            Level = visual ? r.Level ?? 3 : 3
        };
        await supplies.AddAsync(supply);
        await uow.SaveChangesAsync();
        return supply.ToDto();
    }

    public async Task<SupplyDto> RegisterMovementAsync(int id, MovementRequest r)
    {
        Validation.CheckEnum(r.Type, "Tipo de movimiento no válido.");
        var type = r.Type ?? throw new BusinessRuleException("Tipo de movimiento no válido.");
        if (r.Quantity < 0 || (type != MovementType.Adjustment && r.Quantity == 0))
            throw new BusinessRuleException("La cantidad debe ser mayor que cero.");

        // Bloquea la fila del consumible: dos salidas simultáneas no pueden dejar stock negativo.
        await uow.BeginTransactionAsync();
        try
        {
            var supply = await supplies.GetByIdForUpdateAsync(id)
                ?? throw new NotFoundException("No se encontró el consumible.");
            if (supply.Mode != SupplyMode.Quantity)
                throw new BusinessRuleException("Este consumible se controla por nivel visual.");

            switch (type)
            {
                case MovementType.Out:
                    if (r.Quantity > supply.Stock)
                        throw new BusinessRuleException($"Solo hay {supply.Stock} {supply.Unit} en el almacén.");
                    supply.Stock -= r.Quantity;
                    break;
                case MovementType.In:
                    if (r.Quantity > int.MaxValue - supply.Stock)
                        throw new BusinessRuleException("La cantidad es demasiado grande.");
                    supply.Stock += r.Quantity;
                    break;
                default: // Adjustment: fija el stock exacto, incluso en 0.
                    supply.Stock = r.Quantity;
                    break;
            }

            supply.Movements.Add(new SupplyMovement
            {
                At = DateTime.UtcNow, Type = type, Quantity = r.Quantity, ResultingStock = supply.Stock,
                Destination = PeruTime.Clean(r.Destination), User = user.Name
            });
            await uow.SaveChangesAsync();
            await uow.CommitAsync();
            return supply.ToDto();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public async Task<SupplyDto> SetLevelAsync(int id, LevelRequest r)
    {
        if (r.Level is < 0 or > 3)
            throw new BusinessRuleException("El nivel debe estar entre 0 y 3.");

        var supply = await supplies.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró el consumible.");
        if (supply.Mode != SupplyMode.Visual)
            throw new BusinessRuleException("Este consumible se controla por cantidad.");
        supply.Level = r.Level;
        await uow.SaveChangesAsync();
        return supply.ToDto();
    }

    public async Task<List<SupplyMovementDto>> GetMovementsAsync(int id) =>
        (await supplies.GetMovementsAsync(id, 30)).Select(m => m.ToDto()).ToList();
}
