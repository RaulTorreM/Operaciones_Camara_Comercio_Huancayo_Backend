using Inventario.Application.DTOs.Assets;
using Inventario.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Inventario.API.Controllers;

/// <summary>
/// Consulta rápida de un bien por su etiqueta, sin iniciar sesión.
/// Solo lectura y con datos básicos: no expone notas, historial ni fotos,
/// y no permite crear, editar, prestar ni eliminar bienes.
/// </summary>
[ApiController]
[Route("api/inventario/public/assets")]
[AllowAnonymous]
[EnableRateLimiting(PublicRateLimitPolicy)]
public class PublicAssetsController(IAssetService service) : ControllerBase
{
    internal const string PublicRateLimitPolicy = "inventario-public";

    [HttpGet("by-code")]
    public async Task<ActionResult<PublicAssetDto>> GetByCode([FromQuery] string code) =>
        Ok(await service.GetPublicByCodeAsync(code));
}
