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
[Route("api/inventario/assets")]
[Authorize(Policy = InventarioAuth.AdminPolicy)]
public class AssetsController(IAssetService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AssetListItemDto>>> Search(
        [FromQuery] string? q, [FromQuery] int? roomId,
        [FromQuery] VerificationStatus? status, [FromQuery] AssetType? type) =>
        Ok(await service.SearchAsync(q, roomId, status, type));

    [HttpGet("by-code")]
    public async Task<ActionResult<AssetCodeLookupDto>> GetByCode([FromQuery] string code) =>
        Ok(await service.GetByCodeAsync(code));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDetailDto>> Get(int id) => Ok(await service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<CreatedDto>> Create([FromBody] AssetRequest request) =>
        Ok(new CreatedDto(await service.CreateAsync(request)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CreatedDto>> Update(int id, [FromBody] AssetRequest request) =>
        Ok(new CreatedDto(await service.UpdateAsync(id, request)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/verify")]
    public async Task<IActionResult> Verify(int id, [FromBody] VerifyRequest request)
    {
        await service.VerifyAsync(id, request);
        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> Import([FromBody] List<AssetRequest> items) =>
        Ok(await service.ImportAsync(items));
}
