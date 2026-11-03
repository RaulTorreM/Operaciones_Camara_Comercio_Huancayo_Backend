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
[Route("api/inventario/loans")]
[Authorize(Policy = InventarioAuth.AdminPolicy)]
public class LoansController(ILoanService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LoanDto>>> List() => Ok(await service.ListAsync());

    [HttpPost]
    public async Task<ActionResult<CreatedDto>> Create([FromBody] CreateLoanRequest request) =>
        Ok(new CreatedDto(await service.CreateAsync(request)));

    [HttpPost("{id:int}/return")]
    public async Task<ActionResult<ReturnLoanResultDto>> Return(int id, [FromBody] ReturnLoanRequest request) =>
        Ok(await service.ReturnAsync(id, request));
}
