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
using Microsoft.AspNetCore.RateLimiting;

namespace Inventario.API.Controllers;

[ApiController]
[Route("api/inventario/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request) =>
        Ok(await service.LoginAsync(request));
}
