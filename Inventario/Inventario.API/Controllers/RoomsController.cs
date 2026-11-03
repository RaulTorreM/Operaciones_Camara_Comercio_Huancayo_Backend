using Inventario.API.Security;
using Inventario.Application.DTOs.Assets;
using Inventario.Application.DTOs.Auth;
using Inventario.Application.DTOs.Loans;
using Inventario.Application.DTOs.Rooms;
using Inventario.Application.DTOs.Summary;
using Inventario.Application.DTOs.Supplies;
using Inventario.Application.Interfaces;
using Inventario.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventario.API.Controllers;

[ApiController]
[Route("api/inventario/rooms")]
[Authorize(Policy = InventarioAuth.AdminPolicy)]
public class RoomsController(IRoomService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> List() => Ok(await service.ListAsync());

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create([FromBody] RoomRequest request) =>
        Ok(await service.CreateAsync(request));

    [HttpPost("{id:int}/reset-verification")]
    public async Task<ActionResult<ResetResultDto>> ResetVerification(int id) =>
        Ok(await service.ResetVerificationAsync(id));
}
