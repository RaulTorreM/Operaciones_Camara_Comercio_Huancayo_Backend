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
[Route("api/inventario/supplies")]
[Authorize(Policy = InventarioAuth.AdminPolicy)]
public class SuppliesController(ISupplyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SupplyDto>>> List() => Ok(await service.ListAsync());

    [HttpPost]
    public async Task<ActionResult<SupplyDto>> Create([FromBody] SupplyRequest request) =>
        Ok(await service.CreateAsync(request));

    [HttpPost("{id:int}/movements")]
    public async Task<ActionResult<SupplyDto>> RegisterMovement(int id, [FromBody] MovementRequest request) =>
        Ok(await service.RegisterMovementAsync(id, request));

    [HttpPost("{id:int}/level")]
    public async Task<ActionResult<SupplyDto>> SetLevel(int id, [FromBody] LevelRequest request) =>
        Ok(await service.SetLevelAsync(id, request));

    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<List<SupplyMovementDto>>> GetMovements(int id) =>
        Ok(await service.GetMovementsAsync(id));
}
