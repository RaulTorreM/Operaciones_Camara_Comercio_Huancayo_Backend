using DailyTasks.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DailyTasks.API.Hubs;

[Authorize(Policy = DailyTasksAuth.UserPolicy)]
public class DailyTasksHub : Hub
{
}
