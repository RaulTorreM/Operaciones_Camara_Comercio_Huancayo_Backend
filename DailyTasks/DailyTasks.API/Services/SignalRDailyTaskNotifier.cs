using DailyTasks.Application.Interfaces;
using DailyTasks.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DailyTasks.API.Services;

/// <summary>Emite eventos a todos los clientes autenticados del tablero.</summary>
public class SignalRDailyTaskNotifier(
    IHubContext<DailyTasksHub> hub,
    ILogger<SignalRDailyTaskNotifier> logger) : IDailyTaskNotifier
{
    public async Task NotifyAsync(string eventName, object payload, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients.All.SendAsync(eventName, payload, ct);
        }
        catch (Exception ex)
        {
            // Un fallo de notificación no debe romper la operación ya guardada.
            logger.LogWarning(ex, "No se pudo emitir el evento {Event} de DailyTasks", eventName);
        }
    }
}
