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
[Route("api/inventario/summary")]
[Authorize(Policy = InventarioAuth.AdminPolicy)]
public class SummaryController(ISummaryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SummaryDto>> Get() => Ok(await service.GetAsync());
}
