namespace DailyTasks.Core.Entities;

/// <summary>Integrante del módulo DailyTasks. Cada usuario tiene su propia área y rol.</summary>
public class DailyTaskUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Area { get; set; } = "";
    public string Role { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DailyTask> CreatedTasks { get; set; } = new();
    public List<DailyTask> AssignedTasks { get; set; } = new();
    public List<DailyTaskHistory> Changes { get; set; } = new();
}
