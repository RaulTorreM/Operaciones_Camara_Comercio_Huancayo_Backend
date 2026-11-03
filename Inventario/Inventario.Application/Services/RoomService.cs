using Inventario.Application.Common;
using Inventario.Application.DTOs.Rooms;
using Inventario.Application.Interfaces;
using Inventario.Application.Mappings;
using Inventario.Core.Entities;
using Inventario.Core.Exceptions;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class RoomService(IRoomRepository rooms, IUnitOfWork uow) : IRoomService
{
    private const int MaxNameLength = 120;

    public async Task<List<RoomDto>> ListAsync() =>
        (await rooms.ListWithStatsAsync()).Select(r => r.ToDto()).ToList();

    public async Task<RoomDto> CreateAsync(RoomRequest request)
    {
        var name = PeruTime.Clean(request.Name) ?? throw new BusinessRuleException("Escribe el nombre de la sala.");
        Validation.MaxLength(name, MaxNameLength, "El nombre de la sala no debe superar 120 caracteres.");
        if (await rooms.NameExistsAsync(name)) throw new ConflictException("Ya existe una sala con ese nombre.");
        var room = new Room { Name = name };
        await rooms.AddAsync(room);
        await uow.SaveChangesAsync();
        return new RoomDto(room.Id, room.Name, 0, 0, 0);
    }

    /// <summary>Nuevo periodo: deja todos los bienes de la sala como "por verificar".</summary>
    public async Task<ResetResultDto> ResetVerificationAsync(int id)
    {
        _ = await rooms.GetByIdAsync(id) ?? throw new NotFoundException("No se encontró la sala.");
        return new ResetResultDto(await rooms.ResetVerificationAsync(id));
    }
}
