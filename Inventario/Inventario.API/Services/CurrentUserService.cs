using Inventario.Application.Interfaces;

namespace Inventario.API.Services;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Name => accessor.HttpContext?.User.Identity?.Name ?? "sistema";
}
