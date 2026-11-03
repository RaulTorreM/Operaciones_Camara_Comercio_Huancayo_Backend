using System.Security.Claims;
using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace DailyTasks.API.Services;

public class DailyTaskCurrentUserService(IHttpContextAccessor accessor) : IDailyTaskCurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string Name => Principal?.FindFirstValue(ClaimTypes.Name) ?? "sistema";

    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? "";

    public string Area => Principal?.FindFirstValue("area") ?? "";

    public bool IsManager => string.Equals(Role, DailyTaskRoles.Manager, StringComparison.OrdinalIgnoreCase);
}
