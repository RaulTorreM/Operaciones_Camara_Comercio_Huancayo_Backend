namespace DailyTasks.Application.Interfaces;

/// <summary>Emite eventos de cambios para actualizar la interfaz sin recargar.</summary>
public interface IDailyTaskNotifier
{
    Task NotifyAsync(string eventName, object payload, CancellationToken ct = default);
}
