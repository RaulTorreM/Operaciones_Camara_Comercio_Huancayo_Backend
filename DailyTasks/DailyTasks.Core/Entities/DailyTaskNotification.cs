namespace DailyTasks.Core.Entities;

/// <summary>Notificación personal, por ejemplo cuando Gerencia asigna un pendiente.</summary>
public class DailyTaskNotification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
